using System.Collections.Generic;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

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
        /// Uses peak-hold scaling to ensure narrow peaks are always visible.
        /// </summary>
        public void AddWaveform(float[] waveform, uint color) {
            float[] scaled = waveform.Length > Width
                ? GraphUtils.Scale(waveform, Width)
                : waveform;
            List<Band> bands = new List<Band>(scaled.Length);
            for (int i = 0; i < scaled.Length; i++) {
                bands.Add(new Band(i, scaled[i]));
            }
            Equalizer display = new Equalizer(bands, true);
            EndFrequency = scaled.Length;
            AddCurve(display, color);
        }

        /// <inheritdoc/>
        public override void Clear() {
            EndFrequency = 1;
            base.Clear();
        }

        /// <inheritdoc/>
        public override void Normalize() {
            base.Normalize();
            DynamicRange = 2f * Peak;
            ReRenderFull();
        }
    }
}
