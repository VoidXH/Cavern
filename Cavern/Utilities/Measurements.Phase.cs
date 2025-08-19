using System;

namespace Cavern.Utilities {
    // Measurement functions related to phase curves
    public static partial class Measurements {
        /// <summary>
        /// Try to recover the actual <paramref name="phase"/> curve from the results that are confined to the unit circle.
        /// </summary>
        public static void UnwrapPhase(float[] phase) {
            float addition = 0, last = phase[0];
            for (int i = 0; i < phase.Length; i++) {
                float diff = last - phase[i];
                last = phase[i];
                if (Math.Abs(diff) > MathF.PI) {
                    addition += 2 * MathF.PI * Math.Sign(diff);
                }
                phase[i] += addition;
            }
        }

        /// <summary>
        /// Force a phase curve between the [-pi; pi] bounds.
        /// </summary>
        public static void WrapPhase(float[] phase) {
            for (int i = 0; i < phase.Length; i++) {
                phase[i] = (phase[i] + MathF.PI) % (2 * MathF.PI);
                if (phase[i] < 0) {
                    phase[i] += 2 * MathF.PI;
                }
                phase[i] -= MathF.PI;
            }
        }
    }
}
