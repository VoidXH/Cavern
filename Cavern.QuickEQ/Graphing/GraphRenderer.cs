using System;
using System.Collections.Generic;

using Cavern.QuickEQ.Equalization;

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
        public uint[] Pixels { get; }

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
        public void AddCurve(Equalizer curve, uint color) {
            RenderedCurve entry = new RenderedCurve(curve, this) {
                Color = color
            };
            entry.ReRenderFull(this);
            curves.Add(entry);
            DrawSingle(entry);
        }

        /// <summary>
        /// Redraw the entire image.
        /// </summary>
        void DrawAll() {
            Array.Clear(Pixels, 0, Pixels.Length);
            for (int i = 0, c = curves.Count; i < c; i++) {
                DrawSingle(curves[i]);
            }
        }

        /// <summary>
        /// Draw a single curve over the current output.
        /// </summary>
        void DrawSingle(RenderedCurve curve) {
            byte[] source = curve.Render;
            uint r = curve.Color & 0x00FF0000,
                 g = curve.Color & 0x0000FF00,
                 b = curve.Color & 0x000000FF;
            for (int i = 0; i < Pixels.Length; i++) {
                if (source[i] != 0) {
                    byte retain = (byte)(0xFF - source[i]);
                    Pixels[i] = 0xFF000000
                        + ((Pixels[i] & 0x00FF0000) * retain + r * source[i]) / 0xFF
                        + ((Pixels[i] & 0x0000FF00) * retain + g * source[i]) / 0xFF
                        + ((Pixels[i] & 0x000000FF) * retain + b * source[i]) / 0xFF;
                }
            }
        }

        /// <summary>
        /// Re-render but don't regenerate all displayed curves.
        /// </summary>
        void ReRender() {
            for (int i = 0, c = curves.Count; i < c; i++) {
                curves[i].ReRender(this);
            }
            DrawAll();
        }

        /// <summary>
        /// Completely re-render all displayed curves.
        /// </summary>
        void ReRenderFull() {
            for (int i = 0, c = curves.Count; i < c; i++) {
                curves[i].ReRenderFull(this);
            }
            DrawAll();
        }
    }
}