using System.Windows;
using System.Windows.Controls;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Graphing;
using Cavern.QuickEQ.Graphing.Overlays;
using Cavern.WPF.Utils;

namespace Cavern.WPF.Controls;

/// <summary>
/// Displays one or more <see cref="Equalizer"/> filters.
/// </summary>
public partial class GraphRendererControl : UserControl {
    /// <summary>
    /// The background of the displayed graph.
    /// </summary>
    public GraphOverlay Overlay { get; set; } = new LogScaleGrid(2, 1, 0xFF000000, 9);

    /// <summary>
    /// Cavern's internal graph rendering engine.
    /// </summary>
    protected GraphRenderer Renderer { get; private set; } = new(1, 1); // Placeholder for initialization, initial invalidation updates it

    /// <summary>
    /// All displayed curves referencing the current <see cref="Renderer"/>.
    /// </summary>
    readonly List<RenderedCurve> curves = [];

    /// <summary>
    /// Create the renderer of a given size, used on initialization and resize. Overridable to use a derived renderer type.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    protected virtual GraphRenderer CreateRenderer(int width, int height) => new(width, height) {
        DynamicRange = 50,
        Peak = 25,
        Overlay = Overlay
    };

    /// <summary>
    /// Displays one or more <see cref="Equalizer"/> filters.
    /// </summary>
    public GraphRendererControl() => InitializeComponent();

    /// <summary>
    /// Add a curve with an ARGB color.
    /// </summary>
    /// <returns>Index of the curve that can be used in <see cref="Invalidate(int)"/>.</returns>
    public int AddCurve(Equalizer curve, uint color) {
        curves.Add(Renderer.AddCurve(curve, color));
        Invalidate();
        return curves.Count - 1;
    }

    /// <summary>
    /// Remove all displayed curves.
    /// </summary>
    public void Clear() {
        curves.Clear();
        Renderer.Clear();
        Invalidate();
    }

    /// <summary>
    /// Set the <see cref="Renderer"/>'s peak and dynamic range to match the maximum displayed gain.
    /// </summary>
    public void Normalize() {
        Renderer.Normalize();
        InvalidateImage();
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
    public void InvalidateImage() => image.Source = Renderer.ToBitmapSource();

    /// <summary>
    /// Keep the graph's size at the control resolution.
    /// </summary>
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
        base.OnRenderSizeChanged(sizeInfo);
        Renderer = CreateRenderer((int)(sizeInfo.NewSize.Width + .5), (int)(sizeInfo.NewSize.Height + .5));
        for (int i = 0, c = curves.Count; i < c; i++) {
            curves[i] = Renderer.AddCurve(curves[i].Curve, curves[i].Color);
        }
        Invalidate();
    }
}
