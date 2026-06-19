using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Cavern.QuickEQ.Graphing;
using Cavern.WPF.Utils;

namespace Visualize;

/// <summary>
/// Application GUI behavior.
/// </summary>
public partial class MainWindow : Window {
    /// <summary>
    /// Where we're scolled.
    /// </summary>
    double scale = 1;

    /// <summary>
    /// Application GUI behavior.
    /// </summary>
    public MainWindow() {
        InitializeComponent();
        waveform.ItemsSource = Visualizables.Database;
    }

    /// <summary>
    /// When the configuration has changed, redraw the waveform.
    /// </summary>
    void Redraw() {
        if (waveform.SelectedIndex == -1) {
            return;
        }

        WaveformRenderer renderer = new((int)imageHolder.ActualWidth, (int)imageHolder.ActualHeight) {
            Peak = (float)scale,
            DynamicRange = (float)scale * 2,
            Overlay = new Cavern.QuickEQ.Graphing.Overlays.Grid(2, 1, 0xFF000000, 0, 1),
        };

        renderer.AddWaveform(Visualizables.Database[waveform.SelectedIndex].Produce(), 0xFF0000FF);
        image.Source = renderer.ToBitmapSource();
    }

    /// <summary>
    /// When the window is resized, adapt the image.
    /// </summary>
    void OnResize(object _, SizeChangedEventArgs e) {
        if (e.NewSize.Width != 0 && e.NewSize.Height != 0) {
            Redraw();
        }
    }

    /// <summary>
    /// Zoom in on the curve when scrolling.
    /// </summary>
    void OnScroll(object _, MouseWheelEventArgs e) {
        if (e.Delta < 0) {
            scale *= scaleFactor;
        } else {
            scale /= scaleFactor;
        }

        Redraw();
    }

    /// <summary>
    /// The user has selected a new waveform for display.
    /// </summary>
    void OnWaveformChanged(object _, SelectionChangedEventArgs e) => Redraw();

    /// <summary>
    /// How much to change when scrolling
    /// </summary>
    const double scaleFactor = 1.25;
}
