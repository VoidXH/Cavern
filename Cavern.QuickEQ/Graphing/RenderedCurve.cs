using System;
using System.Collections.Generic;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// A curve's source and render.
    /// </summary>
    public class RenderedCurve : ARGBImage {
        /// <summary>
        /// Source curve to draw.
        /// </summary>
        public Equalizer Curve { get; private set; }

        /// <summary>
        /// What graphics to draw over sampling points of the <see cref="Curve"/>.
        /// </summary>
        public ARGBImage PointDisplay {
            get => pointDisplay;
            set {
                pointDisplay = value;
                Update(true);
            }
        }
        ARGBImage pointDisplay;

        /// <summary>
        /// ARGB color of the curve.
        /// </summary>
        /// <remarks>Setting this doesn't re-render the curve, call <see cref="Update(bool)"/> to commit the updates to the graph.</remarks>
        public uint Color { get; set; } = 0xFFFF0000;

        /// <summary>
        /// The graph that draws this curve.
        /// </summary>
        public GraphRenderer Parent { get; }

        /// <summary>
        /// The visualized <see cref="Curve"/>, all of its values at given width values of the <see cref="GraphRenderer"/>.
        /// </summary>
        float[] preRender;

        /// <summary>
        /// True when a sampling point is present in that column.
        /// </summary>
        bool[] pointLocations;

        /// <summary>
        /// A curve's source and render.
        /// </summary>
        public RenderedCurve(Equalizer curve, GraphRenderer parent) : base(parent.Width, parent.Height) {
            Curve = curve;
            Parent = parent;
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
            Array.Clear(Pixels, 0, Pixels.Length);
            float bottom = Parent.Peak - Parent.DynamicRange - Parent.Padding,
                ratio = (Parent.Height - 1) / (Parent.DynamicRange + 2 * Parent.Padding);
            int lastRow = Math.Min((int)((preRender[0] - bottom) * ratio), Parent.Height - 1);
            if (lastRow >= 0 && lastRow < Parent.Height) {
                Pixels[lastRow * Parent.Width] = Color;
            }

            (int drawFrom, int drawTo) = GetDrawingLimits();
            bool drawPoints = PointDisplay != null && pointLocations != null;
            int pointOffset = drawPoints ? pointDisplay.Width / 2 : 0;

            for (; drawFrom < drawTo; drawFrom++) {
                int row = Math.Min((int)((preRender[drawFrom] - bottom) * ratio), Parent.Height - 1);
                for (int j = Math.Max(lastRow, 0); j <= row; j++) {
                    Pixels[j * Parent.Width + drawFrom] = Color;
                }
                for (int j = Math.Max(row, 0); j <= lastRow; j++) {
                    Pixels[j * Parent.Width + drawFrom] = Color;
                }
                lastRow = row;

                if (drawPoints && pointLocations[drawFrom]) {
                    PointDisplay.DrawOver(this, drawFrom - pointOffset, row - pointOffset);
                }
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
                    drawFrom = QMath.RoundToInt(Math.Clamp(fromRelative, 0, 1) * last);
                    drawTo = QMath.RoundToInt(Math.Clamp(toRelative, 0, 1) * last);
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

            if (PointDisplay == null) {
                return;
            }

            pointLocations = new bool[preRender.Length];
            IReadOnlyList<Band> bands = Curve.Bands;
            for (int i = 0, c = bands.Count; i < c; i++) {
                float pixel = Parent.GetWidthAt((float)bands[i].Frequency);
                if (pixel >= 0 && pixel < Width) {
                    pointLocations[(int)pixel] = true;
                }
            }
        }
    }
}
