using System.Collections.Generic;

using Cavern.QuickEQ.Equalization;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Displays waveforms up to 0 dB FS.
    /// </summary>
    public class WaveformRenderer : GraphRenderer {
        /// <inheritdoc/>
        public override DrawableMeasurementType Type => base.Type;

        /// <summary>
        /// Displays waveforms up to 0 dB FS.
        /// </summary>
        public WaveformRenderer(int width, int height) : base(width, height) {
            Peak = 1;
            DynamicRange = 2;
            Logarithmic = false;
            EndFrequency = 1;
        }

        /// <summary>
        /// Add a <paramref name="waveform"/> with an ARGB <paramref name="color"/>.
        /// </summary>
        public void AddWaveform(float[] waveform, uint color) {
            List<Band> bands = new List<Band>(waveform.Length);
            for (int i = 0; i < waveform.Length; i++) {
                bands.Add(new Band(i, waveform[i]));
            }
            Equalizer display = new Equalizer(bands, true);

            if (EndFrequency < waveform.Length) {
                EndFrequency = waveform.Length;
            }

            AddCurve(display, color);
        }

        /// <inheritdoc/>
        public override void Clear() {
            EndFrequency = 1;
            base.Clear();
        }
    }
}
