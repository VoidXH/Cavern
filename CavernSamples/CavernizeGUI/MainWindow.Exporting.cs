using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

using Cavern;
using Cavern.Filters;
using Cavern.Format;
using Cavern.Format.Environment;
using Cavern.Utilities;
using Cavern.Virtualizer;

using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using VoidX.WPF;

namespace CavernizeGUI {
    // Functions that are part of the render export process.
    partial class MainWindow {
        /// <summary>
        /// Keeps track of export time and evaluates performance.
        /// </summary>
        class Progressor(long length, Listener listener, TaskEngine taskEngine) {
            /// <summary>
            /// Samples rendered so far.
            /// </summary>
            public long Rendered { get; private set; }

            /// <summary>
            /// Time of starting the export process.
            /// </summary>
            readonly DateTime start = DateTime.Now;

            /// <summary>
            /// Multiplier of the content length to get the progress ratio.
            /// </summary>
            readonly double samplesToProgress = 1.0 / length;

            /// <summary>
            /// Converts samples to seconds.
            /// </summary>
            readonly double samplesToSeconds = 1.0 / listener.SampleRate;

            /// <summary>
            /// Samples processed each frame.
            /// </summary>
            readonly long updateRate = listener.UpdateRate;

            /// <summary>
            /// UI updater instance.
            /// </summary>
            readonly TaskEngine taskEngine = taskEngine;

            /// <summary>
            /// Samples until next UI update.
            /// </summary>
            long untilUpdate = updateInterval;

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

                    taskEngine.UpdateStatusLazy(string.Format((string)language["ProgP"],
                        progress.ToString("0.00%"), speed.ToString("0.00"), remDisp));
                    taskEngine.UpdateProgressBar(progress);
                    untilUpdate = updateInterval;
                }
            }

            /// <summary>
            /// Report custom progress as finalization.
            /// </summary>
            public void Finalize(double progress) {
                taskEngine.UpdateStatusLazy(string.Format((string)language["FinaP"], progress.ToString("0.00%")));
                taskEngine.UpdateProgressBar(progress);
            }
        }

        /// <summary>
        /// Renders a listener to a file, and returns some measurements of the render.
        /// </summary>
        RenderStats WriteRender(CavernizeTrack target, AudioWriter writer, RenderTarget renderTarget, bool dynamicOnly, bool heightOnly) {
            RenderStats stats = Dispatcher.Invoke(() => grading.IsChecked) ? new RenderStatsEx(listener) : new RenderStats(listener);
            Progressor progressor = new Progressor(target.Length, listener, taskEngine);
            bool customMuting = dynamicOnly || heightOnly;

            MultichannelConvolver filters = null;
            if (FiltersUsed) {
                filters = new MultichannelConvolver(roomCorrection);
            }

            // Virtualization is done with the buffer instead of each update in the listener to optimize FFT sizes
            VirtualizerFilter virtualizer = null;
            Normalizer normalizer = null;
            bool virtualizerState = Listener.HeadphoneVirtualizer;
            if (virtualizerState || Dispatcher.Invoke(() => speakerVirtualizer.IsChecked)) {
                Listener.HeadphoneVirtualizer = false;
                virtualizer = new VirtualizerFilter();
                virtualizer.SetLayout();
                normalizer = new Normalizer(true) {
                    decayFactor = 10 * (float)listener.UpdateRate / listener.SampleRate
                };
            }

            int cachePosition = 0;
            float[] writeCache = null;
            bool flush = false;
            // The size of writeCache makes sure ContainerWriters get correct sized blocks when written with WriteChannelLimitedBlock
            writeCache = new float[blockSize / renderTarget.OutputChannels * Listener.Channels.Length];

#if RELEASE
            bool wasError = false;
#endif
            while (progressor.Rendered < target.Length) {
                float[] result;
#if RELEASE
                try {
#endif
                    result = listener.Render();
#if RELEASE
                } catch (Exception e) {
                    if (!wasError) {
                        wasError = true;
                        ThreadPool.QueueUserWorkItem(x => { // Don't hold up background processing
                            TimeSpan time = TimeSpan.FromSeconds(progressor.Rendered / listener.SampleRate);
                            Dispatcher.Invoke(() => Error(string.Format((string)language["RenEr"], time, e.Message)));
                        });
                    }
                    result = new float[Listener.Channels.Length * listener.UpdateRate];
                }
#endif

                // Alignment of split parts
                if (target.Length - progressor.Rendered < listener.UpdateRate) {
                    Array.Resize(ref result, (int)((target.Length - progressor.Rendered) * Listener.Channels.Length));
                    flush = true;
                }

                Array.Copy(result, 0, writeCache, cachePosition, result.Length);
                cachePosition += result.Length;
                if (cachePosition == writeCache.Length || flush) {
                    filters?.Process(writeCache);

                    if (virtualizer == null) {
                        if (renderTarget is not DownmixedRenderTarget downmix) {
                            writer?.WriteBlock(writeCache, 0, cachePosition);
                        } else {
                            downmix.PerformMerge(writeCache);
                            writer?.WriteChannelLimitedBlock(writeCache, downmix.OutputChannels,
                                Listener.Channels.Length, 0, cachePosition);
                        }
                    } else {
                        virtualizer.Process(writeCache, listener.SampleRate);
                        normalizer.Process(writeCache);
                        writer?.WriteChannelLimitedBlock(writeCache, renderTarget.OutputChannels,
                            Listener.Channels.Length, 0, cachePosition);
                    }
                    cachePosition = 0;
                }

                if (progressor.Rendered > secondFrame) {
                    stats.Update(result);
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
            if (virtualizerState) {
                Listener.HeadphoneVirtualizer = true;
            }
            return stats;
        }

        /// <summary>
        /// Transcodes between object-based tracks, and returns some measurements of the render.
        /// </summary>
        RenderStats WriteTranscode(CavernizeTrack target, EnvironmentWriter writer) {
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
        RenderStats WriteTranscode(CavernizeTrack target, BroadcastWaveFormatWriter writer) {
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