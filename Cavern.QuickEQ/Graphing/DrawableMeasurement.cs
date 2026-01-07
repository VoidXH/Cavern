using System;

using Cavern.QuickEQ.Graphing.Overlays;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Any curve or other kind of measurement that can be drawn to a fixed size image, where the X axis is frequency.
    /// </summary>
    public abstract class DrawableMeasurement {
        /// <summary>
        /// Displayed representation of the measurement data.
        /// </summary>
        public abstract DrawableMeasurementType Type { get; }

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
        /// The data is displayed logarithmically (base 10) on the X axis.
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
        /// Any curve or other kind of measurement that can be drawn to a fixed size image, where the X axis is frequency.
        /// </summary>
        protected DrawableMeasurement(int width, int height) {
            Width = width;
            Height = height;
            Pixels = new uint[width * height];
        }

        /// <summary>
        /// Remove all data sources from the image.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Get the frequency at a given subpixel position on the width axis.
        /// </summary>
        public float GetFrequencyAt(float width) => logarithmic ?
            MathF.Exp(QMath.Lerp(MathF.Log(startFrequency), MathF.Log(endFrequency), width / Width)) :
            QMath.Lerp(startFrequency, endFrequency, width / Width);

        /// <summary>
        /// The inverse of <see cref="GetFrequencyAt"/>, returning what width corresponds to a <paramref name="frequency"/>.
        /// </summary>
        public float GetWidthAt(float frequency) => Width * (logarithmic ?
            QMath.LerpInverse(MathF.Log(startFrequency), MathF.Log(endFrequency), MathF.Log(frequency)) :
            QMath.LerpInverse(startFrequency, endFrequency, frequency));

        /// <summary>
        /// Re-render without regenerating all displayed data.
        /// </summary>
        protected abstract void ReRender();

        /// <summary>
        /// Recalculate and re-render all measurements.
        /// </summary>
        protected abstract void ReRenderFull();
    }
}