using System;
using System.Collections.Generic;
using System.Numerics;

using Cavern;
using Cavern.Filters;
using Cavern.Format;
using Cavern.Format.Environment;
using Cavern.Utilities;
using Cavern.Virtualizer;
using CavernizeGUI.Elements;
using VoidX.WPF;

namespace CavernizeGUI {
    // Functions that are part of the render export process.
    partial class MainWindow {
        /// <summary>
        /// Keeps track of export time and evaluates performance.
        /// </summary>
        class Progressor {
            /// <summary>
            /// Samples rendered so far.
            /// </summary>
            public long Rendered { get; private set; } = 0;

            /// <summary>
            /// Time of starting the export process.
            /// </summary>
            readonly DateTime start = DateTime.Now;

            /// <summary>
            /// Multiplier of the content length to get the progress ratio.
            /// </summary>
            readonly double samplesToProgress;

            /// <summary>
            /// Converts samples to seconds.
            /// </summary>
            readonly double samplesToSeconds;

            /// <summary>
            /// Samples processed each frame.
            /// </summary>
            readonly long updateRate;

            /// <summary>
            /// UI updater instance.
            /// </summary>
            readonly TaskEngine taskEngine;

            /// <summary>
            /// Samples until next UI update.
            /// </summary>
            long untilUpdate = updateInterval;

            /// <summary>
            /// Constructs a progress keeper.
            /// </summary>
            public Progressor(long length, Listener listener, TaskEngine taskEngine) {
                samplesToProgress = 1.0 / length;
                samplesToSeconds = 1.0 / listener.SampleRate;
                updateRate = listener.UpdateRate;
                this.taskEngine = taskEngine;
            }

            /// <summary>
            /// Report progress after each listener update.
            /// </summary>
            public void Update() {
                Rendered += updateRate;
                if ((untilUpdate -= updateRate) <= 0) {
                    double progress = Rendered * samplesToProgress;
                    TimeSpan elapsed = DateTime.Now - start, remaining = elapsed / progress - elapsed;
                    double speed = Rendered * samplesToSeconds / elapsed.TotalSeconds;

                    string remDisp;
                    if (remaining.TotalDays < 1) {
                        if (remaining.TotalHours < 1) {
                            remDisp = remaining.ToString("mm':'ss");
                        } else {
                            remDisp = remaining.ToString("h':'mm':'ss");
                        }
                    } else {
                        remDisp = remaining.ToString("d':'hh':'mm':'ss");
                    }

                    taskEngine.UpdateStatusLazy(string.Format((string)MainWindow.language["ProgP"],
                        progress.ToString("0.00%"), speed.ToString("0.00"), remDisp));
                    taskEngine.UpdateProgressBar(progress);
                    untilUpdate = updateInterval;
                }
            }

            /// <summary>
            /// Report custom progress as finalization.
            /// </summary>
            public void Finalize(double progress) {
                taskEngine.UpdateStatusLazy(string.Format((string)MainWindow.language["FinaP"], progress.ToString("0.00%")));
                taskEngine.UpdateProgressBar(progress);
            }
        }

        /// <summary>
        /// Renders a listener to a file, and returns some measurements of the render.
        /// </summary>
        RenderStats WriteRender(Track target, AudioWriter writer, bool dynamicOnly, bool heightOnly) {
            RenderStats stats = new(listener);
            Progressor progressor = new Progressor(target.Length, listener, taskEngine);
            bool customMuting = dynamicOnly || heightOnly;

            Filter[] filters = null;
            if (roomCorrection != null && Listener.Channels.Length == roomCorrection.Length) {
                filters = new Filter[roomCorrection.Length];
                for (int ch = 0; ch < Listener.Channels.Length; ch++) {
                    filters[ch] = new ThreadSafeFastConvolver(roomCorrection[ch]);
                }
            }

            // Virtualization is done with the buffer instead of each update in the listener to optimize FFT sizes
            VirtualizerFilter virtualizer = null;
            if (Listener.HeadphoneVirtualizer) {
                Listener.HeadphoneVirtualizer = false;
                virtualizer = new VirtualizerFilter();
                virtualizer.SetLayout();
            }

            const int defaultWriteCacheLength = 16384; // Samples per channel
            int cachePosition = 0;
            float[] writeCache = null;
            bool flush = false;
            if (writer != null) {
                int cacheSize = filters == null ? defaultWriteCacheLength : roomCorrection[0].Length;
                if (cacheSize < listener.UpdateRate) {
                    cacheSize = listener.UpdateRate;
                } else if (cacheSize % listener.UpdateRate != 0) {
                    // Cache handling is written to only handle when its size is divisible with the update rate - it's faster this way
                    cacheSize += listener.UpdateRate - cacheSize % listener.UpdateRate;
                }
                cacheSize *= virtualizer != null ? Listener.Channels.Length : VirtualizerFilter.VirtualChannels;
                writeCache = new float[cacheSize];
            }
            // TODO: override multichannel process for the fast convolution filter to prevent reallocation

            while (progressor.Rendered < target.Length) {
                float[] result = listener.Render();

                // Alignment of split parts
                if (target.Length - progressor.Rendered < listener.UpdateRate) {
                    Array.Resize(ref result, (int)((target.Length - progressor.Rendered) * Listener.Channels.Length));
                    flush = true;
                }

                if (writer != null) {
                    Array.Copy(result, 0, writeCache, cachePosition, result.Length);
                    cachePosition += result.Length;
                    if (cachePosition == writeCache.Length || flush) {
                        if (filters != null) {
                            filters.ProcessAllChannels(writeCache);
                        }

                        if (virtualizer == null) {
                            writer.WriteBlock(writeCache, 0, cachePosition);
                        } else {
                            virtualizer.Process(writeCache, listener.SampleRate);
                            writer.WriteChannelLimitedBlock(writeCache, 2, Listener.Channels.Length, 0, cachePosition);
                        }
                        cachePosition = 0;
                    }
                }
                if (progressor.Rendered > secondFrame) {
                    stats.Update();
                }

                if (customMuting) {
                    IReadOnlyList<Source> objects = target.Renderer.Objects;
                    for (int i = 0, c = objects.Count; i < c; ++i) {
                        Vector3 rawPos = objects[i].Position / Listener.EnvironmentSize;
                        objects[i].Mute =
                            (dynamicOnly && MathF.Abs(rawPos.X) % 1 < .01f &&
                            MathF.Abs(rawPos.Y) % 1 < .01f && MathF.Abs(rawPos.Z % 1) < .01f) ||
                            (heightOnly && rawPos.Y == 0);
                    }
                }

                progressor.Update();
            }

            writer?.Dispose();
            if (virtualizer != null) {
                Listener.HeadphoneVirtualizer = true;
            }
            return stats;
        }

        /// <summary>
        /// Transcodes between object-based tracks, and returns some measurements of the render.
        /// </summary>
        RenderStats WriteTranscode(Track target, EnvironmentWriter writer) {
            RenderStats stats = new(listener);
            Progressor progressor = new Progressor(target.Length, listener, taskEngine);

            while (progressor.Rendered < target.Length) {
                writer.WriteNextFrame();
                progressor.Update();
            }

            writer.Dispose();
            return stats;
        }

        /// <summary>
        /// Transcodes from object-based tracks to ADM BWF, and returns some measurements of the render.
        /// </summary>
        RenderStats WriteTranscode(Track target, BroadcastWaveFormatWriter writer) {
            RenderStats stats = new(listener);
            Progressor progressor = new Progressor((long)(target.Length / progressSplit), listener, taskEngine);

            while (progressor.Rendered < target.Length) {
                writer.WriteNextFrame();
                progressor.Update();
            }

            writer.FinalFeedback = progressor.Finalize;
            writer.FinalFeedbackStart = progressSplit;
            writer.Dispose();
            return stats;
        }

        /// <summary>
        /// Update the UI after this many samples have been rendered.
        /// </summary>
        const long updateInterval = 50000;

        /// <summary>
        /// The OAMD objects need this many samples at max to move to their initial position.
        /// </summary>
        const int secondFrame = 2 * 1536;

        /// <summary>
        /// The export process exports PCM data until this percentage and extra metadata after that.
        /// </summary>
        const double progressSplit = .95;
    }
}