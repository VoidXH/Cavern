using Cavern.MAUI.Utilities;
using Cavern.QuickEQ.Graphing;

using Grid = Cavern.QuickEQ.Graphing.Overlays.Grid;

namespace Cavern.MAUI.Controls;

/// <summary>
/// Displays a <see cref="GraphRenderer"/> with automatically added labels.
/// </summary>
public partial class LabeledGraph : ContentView {
    /// <summary>
    /// Width of the drawn curve excluding the labels.
    /// </summary>
    public double ImageWidth {
        get => image.WidthRequest;
        set => image.WidthRequest = value;
    }

    /// <summary>
    /// Height of the drawn curve excluding the labels.
    /// </summary>
    public double ImageHeight {
        get => image.HeightRequest;
        set => image.HeightRequest = value;
    }

    /// <summary>
    /// Unit displayed for X-axis gridlines.
    /// </summary>
    public string XUnit {
        get => xUnit;
        set {
            xUnit = value;
            Redraw();
        }
    }
    string xUnit = "0 Hz";

    /// <summary>
    /// Unit displayed for Y-axis gridlines.
    /// </summary>
    public string YUnit {
        get => yUnit;
        set {
            yUnit = value;
            Redraw();
        }
    }
    string yUnit = "0 dB";

    /// <summary>
    /// The displayed graph.
    /// </summary>
    GraphRenderer renderer;

    /// <summary>
    /// Displays a <see cref="GraphRenderer"/> with automatically added labels.
    /// </summary>
    public LabeledGraph() => InitializeComponent();

    /// <summary>
    /// Display a preassembled graph.
    /// </summary>
    public void SetGraph(GraphRenderer renderer) {
        this.renderer = renderer;
        image.Source = renderer.ToImageSource();
        Redraw();
    }

    /// <summary>
    /// Display the graph again when a property has changed.
    /// </summary>
    void Redraw() {
        if (renderer == null || renderer.Overlay is not Grid grid) {
            return;
        }

        xAxis.Children.Clear();
        int steps = grid.XSteps + 1;
        float mul = (float)renderer.Width / steps;
        for (int i = 0; i <= steps; i++) {
            Label label = new() {
                Text = renderer.GetFrequencyAt(i * mul).ToString(xUnit)
            };
            xAxis.Add(label);
        }

        yAxis.Children.Clear();
        steps = grid.YSteps + 1;
        mul = (float)renderer.Height / steps;
        for (int i = 0; i <= steps; i++) {
            Label label = new() {
                Text = renderer.GetGainAt(i * mul).ToString(yUnit),
                HorizontalTextAlignment = TextAlignment.End
            };
            yAxis.Add(label);
        }
    }
}
