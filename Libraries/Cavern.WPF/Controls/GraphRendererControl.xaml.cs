using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Graphing;
using Cavern.QuickEQ.Graphing.Overlays;
using Cavern.WPF.Controls.Models;
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
    /// All displayed curves referencing the current <see cref="Renderer"/>, with their legend names.
    /// </summary>
    readonly List<(RenderedCurve Curve, string Name)> curves = [];

    /// <summary>
    /// Show a legend of the named curves.
    /// </summary>
    public bool ShowLegend {
        get => showLegend;
        set {
            showLegend = value;
            UpdateLegendVisibility();
        }
    }
    bool showLegend;

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
    public int AddCurve(Equalizer curve, uint color) => AddCurve(curve, color, null);

    /// <summary>
    /// Add a curve with an ARGB color and a <paramref name="name"/> to display in the legend.
    /// </summary>
    /// <returns>Index of the curve that can be used in <see cref="Invalidate(int)"/>.</returns>
    public int AddCurve(Equalizer curve, uint color, string name) {
        curves.Add((Renderer.AddCurve(curve, color), name));
        UpdateLegend();
        Invalidate();
        return curves.Count - 1;
    }

    /// <summary>
    /// Remove all displayed curves.
    /// </summary>
    public void Clear() {
        curves.Clear();
        Renderer.Clear();
        UpdateLegend();
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
        curves[index].Curve.Update(true);
        InvalidateImage();
    }

    /// <summary>
    /// Update all data related to the graph and redraw.
    /// </summary>
    public void Invalidate() {
        for (int i = 0, c = curves.Count - 1; i <= c; i++) {
            curves[i].Curve.Update(i == c);
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
            (RenderedCurve curve, string name) = curves[i];
            curves[i] = (Renderer.AddCurve(curve.Curve, curve.Color), name);
        }
        Invalidate();
    }

    /// <summary>
    /// Update the legend with the names and colors of the displayed curves.
    /// </summary>
    void UpdateLegend() {
        List<LegendEntry> entries = [];
        foreach ((RenderedCurve curve, string name) in curves) {
            if (name != null) {
                entries.Add(new LegendEntry(name, new SolidColorBrush(ColorUtils.ToColor(curve.Color))));
            }
        }
        legend.ItemsSource = entries;
        UpdateLegendVisibility();
    }

    /// <summary>
    /// Update the legend's visibility: shown when requested and there are named curves.
    /// </summary>
    void UpdateLegendVisibility() {
        Visibility visibility = showLegend && legend.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        legend.Visibility = visibility;
        legendBorder.Visibility = visibility;
    }
}
