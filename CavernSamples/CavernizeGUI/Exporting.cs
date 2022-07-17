using Cavern;
using Cavern.Format;
using Cavern.Utilities;
using System;
using System.Collections.Generic;
using System.Numerics;
using VoidX.WPF;

namespace CavernizeGUI {
    /// <summary>
    /// Functions that are part of the render export process.
    /// </summary>
    static class Exporting {

        /// <summary>
        /// The OAMD objects need this many samples at max to move to their initial position.
        /// </summary>
        const int firstFrame = 1536;

        /// <summary>
        /// Renders a listener to a file, and returns some measurements of the render.
        /// </summary>
        public static RenderStats WriteRender(Listener listener, Track target, AudioWriter writer,
            TaskEngine taskEngine, bool dynamicOnly, bool heightOnly) {
            RenderStats stats = new(listener);
            const long updateInterval = 50000;
            long rendered = 0,
                untilUpdate = updateInterval;
            double samplesToProgress = 1.0 / target.Length,
                samplesToSeconds = 1.0 / listener.SampleRate;
            bool customMuting = dynamicOnly || heightOnly;
            DateTime start = DateTime.Now;

            while (rendered < target.Length) {
                float[] result = listener.Render();

#if DEBUG
                if (rendered > 2500000 && WaveformUtils.GetPeakSigned(result) > .5f)
                    ; // TODO: debug, Amaze will follow with a heavy gain frame and then a normal frame after this detection
#endif
                if (target.Length - rendered < listener.UpdateRate)
                    Array.Resize(ref result, (int)(target.Length - rendered));
                if (writer != null)
                    writer.WriteBlock(result, 0, result.LongLength);
                if (rendered > firstFrame)
                    stats.Update();

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

                rendered += listener.UpdateRate;
                if ((untilUpdate -= listener.UpdateRate) <= 0) {
                    double progress = rendered * samplesToProgress;
                    double speed = rendered * samplesToSeconds / (DateTime.Now - start).TotalSeconds;
                    taskEngine.UpdateStatusLazy($"Rendering... ({progress:0.00%}, speed: {speed:0.00}x)");
                    taskEngine.UpdateProgressBar(progress);
                    untilUpdate = updateInterval;
                }
            }

            if (writer != null)
                writer.Dispose();
            return stats;
        }
    }
}