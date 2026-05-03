using System;
using System.Collections.Generic;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Graphing.Overlays;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Draws multiple <see cref="Equalizer"/> curves on a single canvas.
    /// </summary>
    public class GraphRenderer : DrawableMeasurement {
        /// <inheritdoc/>
        public override DrawableMeasurementType Type => DrawableMeasurementType.Graph;

        /// <summary>
        /// The difference between the highest and lowest displayed values in decibels.
        /// </summary>
        public float DynamicRange {
            get => dynamicRange;
            set {
                dynamicRange = value;
                ReRender();
            }
        }
        float dynamicRange = 30;

        /// <summary>
        /// Maximum value to display in decibels.
        /// </summary>
        public float Peak {
            get => peak;
            set {
                peak = value;
                ReRender();
            }
        }
        float peak = 6;

        /// <summary>
        /// Show a constant value over the limits of the displayed curve.
        /// </summary>
        public bool Extend {
            get => extend;
            set {
                extend = value;
                ReRender();
            }
        }
        bool extend = true;

        /// <summary>
        /// The top and bottom will be moved towards the center by this many decibels.
        /// </summary>
        public float Padding {
            get {
                // The default padding is just for preventing the frame overwriting the curve
                if (padding == 0 && Overlay is Frame frame) {
                    padding = dynamicRange - dynamicRange * Height / (Height + 2 * frame.Width);
                }
                return padding;
            }
            set {
                padding = value;
                ReRender();
            }
        }
        float padding;

        /// <summary>
        /// List of displayed curves in overdrawing order.
        /// </summary>
        readonly List<RenderedCurve> curves = new List<RenderedCurve>();

        /// <summary>
        /// Draws multiple <see cref="Equalizer"/> curves on a single canvas.
        /// </summary>
        public GraphRenderer(int width, int height) : base(width, height) { }

        /// <summary>
        /// Draws multiple <see cref="Equalizer"/> curves on a single canvas.
        /// </summary>
        public GraphRenderer(int width, int height, float startFreq, float endFreq, bool logarithmic) : base(width, height, startFreq, endFreq, logarithmic) { }

        /// <inheritdoc/>
        public override void Clear() {
            curves.Clear();
            DrawAll();
        }

        /// <summary>
        /// Add a curve with an ARGB color.
        /// </summary>
        public RenderedCurve AddCurve(Equalizer curve, uint color) => AddCurve(curve, color, true);

        /// <summary>
        /// Add multiple curves with their respective ARGB colors.
        /// </summary>
        /// <param name="curves">Curves to draw with their corresponding colors</param>
        /// <param name="normalize">Set the <see cref="Peak"/> to the highest value of any curve</param>
        /// <remarks>This is the recommended method for adding multiple curves at once as this doesn't re-render for each one.</remarks>
        public void AddCurves((Equalizer curve, uint color)[] curves, bool normalize) {
            for (int i = 0; i < curves.Length; i++) {
                AddCurve(curves[i].curve, curves[i].color, false);
            }
            if (normalize) {
                Normalize();
            } else {
                ReRenderFull();
            }
        }

        /// <summary>
        /// Get what gain corresponds to a given subpixel position on the height axis, relative to the peak gain.
        /// </summary>
        public float GetGainAt(float height) => peak - height / Height * dynamicRange;

        /// <summary>
        /// Set the <see cref="peak"/> to the max displayed gain.
        /// </summary>
        public void Normalize() {
            double curvePeak = float.NegativeInfinity;
            for (int i = 0, c = curves.Count; i < c; i++) {
                IReadOnlyList<Band> curve = curves[i].Curve.Bands;
                for (int f = 0, c2 = curve.Count; f < c2; f++) {
                    if (curvePeak < curve[f].Gain) {
                        curvePeak = curve[f].Gain;
                    }
                }
            }
            Peak = (float)curvePeak;
            ReRenderFull();
        }

        /// <inheritdoc/>
        protected override void ReRender() {
            int c = curves.Count;
            if (c == 0) {
                for (int i = 0; i < c; i++) {
                    curves[i].ReRender();
                }
            }
            DrawAll();
        }

        /// <inheritdoc/>
        protected override void ReRenderFull() {
            for (int i = 0, c = curves.Count; i < c; i++) {
                curves[i].ReRenderFull();
            }
            DrawAll();
        }

        /// <summary>
        /// Redraw the entire image.
        /// </summary>
        internal void DrawAll() {
            Array.Clear(Pixels, 0, Pixels.Length);
            Overlay?.DrawBehind(this);
            for (int i = 0, c = curves.Count; i < c; i++) {
                curves[i].DrawOver(this, 0, 0);
            }
            Overlay?.DrawOn(this);
        }

        /// <summary>
        /// Add a curve with an ARGB color, with the option not to draw it instantly.
        /// </summary>
        RenderedCurve AddCurve(Equalizer curve, uint color, bool redraw) {
            RenderedCurve entry = new RenderedCurve(curve, this) {
                Color = color
            };
            curves.Add(entry);
            if (redraw) {
                entry.ReRenderFull();
                DrawAll();
            }
            return entry;
        }
    }
}
