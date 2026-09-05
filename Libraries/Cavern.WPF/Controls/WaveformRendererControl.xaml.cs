using Cavern.QuickEQ.Graphing;
using Cavern.QuickEQ.Graphing.Overlays;

namespace Cavern.WPF.Controls {
    /// <summary>
    /// Displays a single waveform up to 0 dB FS, inheriting from <see cref="GraphRendererControl"/>.
    /// </summary>
    public class WaveformRendererControl : GraphRendererControl {
        /// <summary>
        /// Creates a new instance of the control with no waveform loaded.
        /// </summary>
        public WaveformRendererControl() {
            InitializeComponent();
            Overlay = new Frame(1, 0xFF000000);
        }

        /// <summary>
        /// Create a <see cref="WaveformRenderer"/> instead of the default <see cref="GraphRenderer"/>.
        /// </summary>
        /// <param name="width">Width in pixels.</param>
        /// <param name="height">Height in pixels.</param>
        protected override GraphRenderer CreateRenderer(int width, int height) => new WaveformRenderer(width, height) {
            Overlay = Overlay
        };

        /// <summary>
        /// Add a new waveform and redraw.
        /// </summary>
        /// <param name="waveform">The waveform samples to display.</param>
        /// <param name="color">ARGB color of the waveform.</param>
        public void AddWaveform(float[] waveform, uint color) {
            ((WaveformRenderer)Renderer).AddWaveform(waveform, color);
            Invalidate();
        }
    }
}
