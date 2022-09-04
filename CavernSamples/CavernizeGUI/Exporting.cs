using System;
using System.Collections.Generic;
using System.Numerics;

using Cavern;
using Cavern.Format;
using Cavern.Format.Environment;
using Cavern.Utilities;
using VoidX.WPF;

namespace CavernizeGUI {
    /// <summary>
    /// Functions that are part of the render export process.
    /// </summary>
    static class Exporting {
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
                    double speed = Rendered * samplesToSeconds / (DateTime.Now - start).TotalSeconds;
                    taskEngine.UpdateStatusLazy($"Rendering... ({progress:0.00%}, speed: {speed:0.00}x)");
                    taskEngine.UpdateProgressBar(progress);
                    untilUpdate = updateInterval;
                }
            }
        }

        /// <summary>
        /// Renders a listener to a file, and returns some measurements of the render.
        /// </summary>
        public static RenderStats WriteRender(Listener listener, Track target, AudioWriter writer,
            TaskEngine taskEngine, bool dynamicOnly, bool heightOnly) {
            RenderStats stats = new(listener);
            Progressor progressor = new Progressor(target.Length, listener, taskEngine);
            bool customMuting = dynamicOnly || heightOnly;

            while (progressor.Rendered < target.Length) {
                float[] result = listener.Render();

#if DEBUG
                if (progressor.Rendered > 2500000 && WaveformUtils.GetPeakSigned(result) > .5f)
                    ; // TODO: debug, Amaze will follow with a heavy gain frame and then a normal frame after this detection
#endif
                // Alignment of split parts
                if (target.Length - progressor.Rendered < listener.UpdateRate) {
                    Array.Resize(ref result, (int)((target.Length - progressor.Rendered) * Listener.Channels.Length));
                }

                writer?.WriteBlock(result, 0, result.LongLength);
                if (progressor.Rendered > firstFrame) {
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
            return stats;
        }

        /// <summary>
        /// Transcodes between object-based tracks, and returns some measurements of the render.
        /// </summary>
        public static RenderStats WriteTranscode(Listener listener, Track target, EnvironmentWriter writer,
            TaskEngine taskEngine) {
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
        /// Update the UI after this many samples have been rendered.
        /// </summary>
        const long updateInterval = 50000;

        /// <summary>
        /// The OAMD objects need this many samples at max to move to their initial position.
        /// </summary>
        const int firstFrame = 1536;
    }
}