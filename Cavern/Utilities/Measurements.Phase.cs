using System;

namespace Cavern.Utilities {
    // Measurement functions related to phase curves
    partial class Measurements {
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
    }
}
