using System;

using Cavern.Utilities;

namespace Cavern.Waveforms.Utilities {
    /// <summary>
    /// Given a <see cref="mapping"/>, move the original channels to those positions.
    /// </summary>
    public class InterlacedWaveformReorderer {
        /// <summary>
        /// Where to move each source channel (array index = original channel, value = new channel).
        /// When null, <see cref="Process(float[])"/> does nothing.
        /// </summary>
        readonly int[] mapping;

        /// <summary>
        /// Working array when copying between channels.
        /// </summary>
        float[] temp = Array.Empty<float>();

        /// <summary>
        /// Given a <see cref="mapping"/>, move the original channels to those positions.
        /// </summary>
        public InterlacedWaveformReorderer(int[] mapping) {
            for (int i = 0; i < mapping.Length; i++) {
                if (mapping[i] != i) {
                    this.mapping = mapping; // Only keep the mapping if it actualy does remapping
                    return;
                }
            }
        }

        /// <summary>
        /// Reorder the interlaced channels in the source <paramref name="samples"/>.
        /// </summary>
        public void Process(float[] samples) {
            if (mapping == null) {
                return;
            }

            if (temp.Length != samples.Length) {
                temp = new float[samples.Length];
            }

            Array.Copy(samples, temp, samples.Length);
            for (int i = 0; i < mapping.Length; i++) {
                if (mapping[i] != i) {
                    WaveformUtils.ExtractChannel(temp, i, mapping.Length, samples, mapping[i], mapping.Length);
                }
            }
        }
    }
}
