using System;
using System.Collections.Generic;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Graphing.Overlays;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Draws multiple <see cref="Equalizer"/> curves on a single canvas.
    /// </summary>
    public class GraphRenderer {
        /// <summary>
        /// First frequency to display.
        /// </summary>
        public float StartFrequency {
            get => startFrequency;
            set {
                startFrequency = value;
                ReRenderFull();
            }
        }
        float startFrequency = 20;

        /// <summary>
        /// Last frequency to display.
        /// </summary>
        public float EndFrequency {
            get => endFrequency;
            set {
                endFrequency = value;
                ReRenderFull();
            }
        }
        float endFrequency = 20000;

        /// <summary>
        /// The curves are displayed logarithmically (base 10).
        /// </summary>
        public bool Logarithmic {
            get => logarithmic;
            set {
                logarithmic = value;
                ReRenderFull();
            }
        }
        bool logarithmic = true;

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
        /// Canvas width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Canvas height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// ARGB values for all pixels of the canvas, line by line.
        /// </summary>
        public uint[] Pixels { get; } = Array.Empty<uint>();

        /// <summary>
        /// Something to draw over the graph, like a <see cref="Frame"/> or <see cref="Grid"/>.
        /// </summary>
        public GraphOverlay Overlay { get; set; }

        /// <summary>
        /// List of displayed curves in overdrawing order.
        /// </summary>
        readonly List<RenderedCurve> curves = new List<RenderedCurve>();

        /// <summary>
        /// Draws multiple <see cref="Equalizer"/> curves on a single canvas.
        /// </summary>
        public GraphRenderer(int width, int height) {
            Width = width;
            Height = height;
            Pixels = new uint[width * height];
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
        /// Remove all curves from the graph.
        /// </summary>
        public void Clear() {
            curves.Clear();
            DrawAll();
        }

        /// <summary>
        /// Get the frequency at a given subpixel position on the width axis.
        /// </summary>
        public float GetFrequencyAt(float width) {
            if (logarithmic) {
                float start = MathF.Log10(startFrequency);
                return MathF.Pow(10, start + width / Width * (MathF.Log10(endFrequency) - start));
            } else {
                return startFrequency + width / Width * (endFrequency - startFrequency);
            }
        }

        /// <summary>
        /// Get what gain corresponds to a given subpixel position on the heigh axis, relative to the peak gain.
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

        /// <summary>
        /// Redraw the entire image.
        /// </summary>
        internal void DrawAll() {
            Array.Clear(Pixels, 0, Pixels.Length);
            Overlay?.DrawBehind(this);
            for (int i = 0, c = curves.Count; i < c; i++) {
                DrawSingle(curves[i]);
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
                DrawSingle(entry);
                Overlay?.DrawOn(this);
            }
            return entry;
        }

        /// <summary>
        /// Draw a single curve over the current output.
        /// </summary>
        unsafe void DrawSingle(RenderedCurve curve) {
            uint r = curve.Color & 0x00FF0000,
                 g = curve.Color & 0x0000FF00,
                 b = curve.Color & 0x000000FF;
            fixed (byte* pSource = curve.Render)
            fixed (uint* pPixels = Pixels) {
                byte* source = pSource, end = source + Pixels.Length;
                uint* pixel = pPixels;
                while (source != end) {
                    if (*source != 0) {
                        byte retain = (byte)(0xFF - *source);
                        *pixel = 0xFF000000
                            | ((*pixel & 0x00FF0000) * retain + r * *source) >> 8
                            | ((*pixel & 0x0000FF00) * retain + g * *source) >> 8
                            | ((*pixel & 0x000000FF) * retain + b * *source) >> 8;
                    }
                    source++;
                    pixel++;
                }
            }
        }

        /// <summary>
        /// Re-render but don't regenerate all displayed curves.
        /// </summary>
        void ReRender() {
            int c = curves.Count;
            if (c == 0) {
                for (int i = 0; i < c; i++) {
                    curves[i].ReRender();
                }
            }
            DrawAll();
        }

        /// <summary>
        /// Completely re-render all displayed curves.
        /// </summary>
        void ReRenderFull() {
            for (int i = 0, c = curves.Count; i < c; i++) {
                curves[i].ReRenderFull();
            }
            DrawAll();
        }
    }
}