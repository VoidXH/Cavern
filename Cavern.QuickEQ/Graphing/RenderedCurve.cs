using System;

using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// A curve's source and render.
    /// </summary>
    public class RenderedCurve {
        /// <summary>
        /// Source curve to draw.
        /// </summary>
        public Equalizer Curve { get; private set; }

        /// <summary>
        /// The graph that draws this curve.
        /// </summary>
        public GraphRenderer Parent { get; }

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
            Parent = parent;
            Render = new byte[parent.Width * parent.Height];
            ReRenderFull();
        }

        /// <summary>
        /// Apply the changes to the set <see cref="Curve"/> to the displayed graph and optionally <paramref name="redraw"/> it.
        /// </summary>
        public void Update(bool redraw) {
            ReRenderFull();
            if (redraw) {
                Parent.DrawAll();
            }
        }

        /// <summary>
        /// Change the <paramref name="curve"/> displayed by this unit of the graph,
        /// and optionally <paramref name="redraw"/> the entire graph.
        /// </summary>
        public void Update(Equalizer curve, bool redraw) {
            Curve = curve;
            Update(redraw);
        }

        /// <summary>
        /// Some minor values have changed, recreate the <see cref="Render"/> from the <see cref="preRender"/>.
        /// </summary>
        internal void ReRender() {
            Array.Clear(Render, 0, Render.Length);
            float bottom = Parent.Peak - Parent.DynamicRange - Parent.Padding,
                ratio = (Parent.Height - 1) / (Parent.DynamicRange + 2 * Parent.Padding);
            int lastRow = Math.Min((int)((preRender[0] - bottom) * ratio), Parent.Height - 1);
            if (lastRow >= 0 && lastRow < Parent.Height) {
                Render[lastRow * Parent.Width] = 0xFF;
            }

            (int drawFrom, int drawTo) = GetDrawingLimits();
            for (; drawFrom < drawTo; drawFrom++) {
                int row = Math.Min((int)((preRender[drawFrom] - bottom) * ratio), Parent.Height - 1);
                for (int j = Math.Max(lastRow, 0); j <= row; j++) {
                    Render[j * Parent.Width + drawFrom] = 0xFF;
                }
                for (int j = Math.Max(row, 0); j <= lastRow; j++) {
                    Render[j * Parent.Width + drawFrom] = 0xFF;
                }
                lastRow = row;
            }
        }

        /// <summary>
        /// Major values have changed (like frequency limits), restart from <see cref="preRender"/>ing.
        /// </summary>
        internal void ReRenderFull() {
            Update();
            ReRender();
        }

        /// <summary>
        /// Get the first (inclusive) and last (exclusive) column that contains pixels, depending on the <see cref="Parent"/>'s settings.
        /// </summary>
        (int drawFrom, int drawTo) GetDrawingLimits() {
            int drawFrom = 1;
            int drawTo = preRender.Length;
            if (!Parent.Extend) {
                if (Curve.Bands.Count == 0) {
                    drawFrom = drawTo;
                } else {
                    double fromRelative, toRelative;
                    if (Parent.Logarithmic) {
                        fromRelative = QMath.LorpInverse(Parent.StartFrequency, Parent.EndFrequency, Curve.StartFrequency);
                        toRelative = QMath.LorpInverse(Parent.StartFrequency, Parent.EndFrequency, Curve.EndFrequency);
                    } else {
                        fromRelative = QMath.LerpInverse(Parent.StartFrequency, Parent.EndFrequency, Curve.StartFrequency);
                        toRelative = QMath.LerpInverse(Parent.StartFrequency, Parent.EndFrequency, Curve.EndFrequency);
                    }

                    int last = Parent.Width - 1;
                    drawFrom = (int)(Math.Clamp(fromRelative, 0, 1) * last + .5);
                    drawTo = (int)(Math.Clamp(toRelative, 0, 1) * last + .5);
                }
            }
            return (drawFrom, drawTo);
        }

        /// <summary>
        /// Update the pixel positions.
        /// </summary>
        void Update() {
            preRender = Parent.Logarithmic ?
                Curve.Visualize(Parent.StartFrequency, Parent.EndFrequency, Parent.Width) :
                Curve.VisualizeLinear(Parent.StartFrequency, Parent.EndFrequency, Parent.Width);
        }
    }
}
