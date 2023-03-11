using System;

using Cavern.QuickEQ.Equalization;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// A curve's source and render.
    /// </summary>
    class RenderedCurve {
        /// <summary>
        /// Source curve to draw.
        /// </summary>
        public Equalizer Curve { get; }

        /// <summary>
        /// ARGB color of the curve.
        /// </summary>
        public uint Color { get; set; } = 0xFFFF0000;

        /// <summary>
        /// The brightness value for each pixel on the output.
        /// </summary>
        public byte[] Render { get; }

        /// <summary>
        /// The visualized <see cref="Curve"/>, all of its values at given width values of the <see cref="GraphRenderer"/>.
        /// </summary>
        float[] preRender;

        /// <summary>
        /// A curve's source and render.
        /// </summary>
        public RenderedCurve(Equalizer curve, GraphRenderer parent) {
            Curve = curve;
            Render = new byte[parent.Width * parent.Height];
            ReRenderFull(parent);
        }

        /// <summary>
        /// Some minor values have changed, recreate the <see cref="Render"/> from the <see cref="preRender"/>.
        /// </summary>
        public void ReRender(GraphRenderer parent) {
            float bottom = parent.Peak - parent.DynamicRange - parent.Padding,
                ratio = (parent.Height - 1) / (parent.DynamicRange + 2 * parent.Padding);
            int lastRow = Math.Min((int)((preRender[0] - bottom) * ratio), parent.Height - 1);
            if (lastRow >= 0 && lastRow < parent.Height) {
                Render[lastRow * parent.Width] = 0xFF;
            }
            for (int i = 1; i < preRender.Length; i++) {
                int row = Math.Min((int)((preRender[i] - bottom) * ratio), parent.Height - 1);
                for (int j = Math.Max(lastRow, 0); j <= row; j++) {
                    Render[j * parent.Width + i] = 0xFF;
                }
                for (int j = Math.Max(row, 0); j <= lastRow; j++) {
                    Render[j * parent.Width + i] = 0xFF;
                }
                lastRow = row;
            }
        }

        /// <summary>
        /// Major values have changed (like frequency limits), restart from <see cref="preRender"/>ing.
        /// </summary>
        public void ReRenderFull(GraphRenderer parent) {
            preRender = parent.Logarithmic ?
                Curve.Visualize(parent.StartFrequency, parent.EndFrequency, parent.Width) :
                Curve.VisualizeLinear(parent.StartFrequency, parent.EndFrequency, parent.Width);
            ReRender(parent);
        }
    }
}