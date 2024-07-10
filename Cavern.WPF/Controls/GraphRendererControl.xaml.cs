using System.Windows;
using System.Windows.Controls;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Graphing;
using Cavern.QuickEQ.Graphing.Overlays;
using Cavern.WPF.Utils;

namespace Cavern.WPF.Controls {
    /// <summary>
    /// Displays one or more <see cref="Equalizer"/> filters.
    /// </summary>
    public partial class GraphRendererControl : UserControl {
        /// <summary>
        /// The background of the displayed graph.
        /// </summary>
        public GraphOverlay Overlay { get; set; } = new LogScaleGrid(2, 1, 0xFF000000, 10);

        /// <summary>
        /// All displayed curves referencing the current <see cref="renderer"/>.
        /// </summary>
        readonly List<RenderedCurve> curves = [];

        /// <summary>
        /// Cavern's internal graph rendering engine.
        /// </summary>
        GraphRenderer renderer = new GraphRenderer(1, 1); // Placeholder for initialization, initial invalidation updates it

        /// <summary>
        /// Displays one or more <see cref="Equalizer"/> filters.
        /// </summary>
        public GraphRendererControl() => InitializeComponent();

        /// <summary>
        /// Add a curve with an ARGB color.
        /// </summary>
        /// <returns>Index of the curve that can be used in <see cref="Invalidate(int)"/>.</returns>
        public int AddCurve(Equalizer curve, uint color) {
            curves.Add(renderer.AddCurve(curve, color));
            Invalidate();
            return curves.Count - 1;
        }

        /// <summary>
        /// Remove all displayed curves.
        /// </summary>
        public void Clear() {
            curves.Clear();
            renderer.Clear();
            Invalidate();
        }

        /// <summary>
        /// When a curve at a given <paramref name="index"/> has changed, update its drawn curve.
        /// </summary>
        public void Invalidate(int index) {
            curves[index].Update(true);
            InvalidateImage();
        }

        /// <summary>
        /// Update all data related to the graph and redraw.
        /// </summary>
        public void Invalidate() {
            for (int i = 0, c = curves.Count - 1; i <= c; i++) {
                curves[i].Update(i == c);
            }
            InvalidateImage();
        }

        /// <summary>
        /// Update the displayed graph when a curve was added, changed, or removed.
        /// </summary>
        public void InvalidateImage() => image.Source = renderer.Pixels.ToBitmap(renderer.Width, renderer.Height).ToImageSource();

        /// <summary>
        /// Keep the graph's size at the control resolution.
        /// </summary>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
            base.OnRenderSizeChanged(sizeInfo);
            renderer = new((int)(sizeInfo.NewSize.Width + .5), (int)(sizeInfo.NewSize.Height + .5)) {
                DynamicRange = 50,
                Peak = 25,
                Overlay = Overlay
            };
            for (int i = 0, c = curves.Count; i < c; i++) {
                curves[i] = renderer.AddCurve(curves[i].Curve, curves[i].Color);
            }
            Invalidate();
        }
    }
}