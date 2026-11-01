using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Cavern.QuickEQ.Graphing;
using Cavern.WPF.Utils;
using Cavern.Format;
using Cavern.Utilities;
using Cavern.Waveforms;

namespace Visualize;

/// <summary>
/// Application GUI behavior.
/// </summary>
public partial class MainWindow : Window {
    /// <summary>
    /// Where we're scrolled.
    /// </summary>
    double scale = 1;

    /// <summary>
    /// A loaded waveform.
    /// </summary>
    float[] audioFile;

    /// <summary>
    /// Sample rate of the loaded <see cref="audioFile"/>.
    /// </summary>
    int sampleRate;

    /// <summary>
    /// Possible waveforms to display.
    /// </summary>
    Visualizable[] visualizables;

    /// <summary>
    /// Application GUI behavior.
    /// </summary>
    public MainWindow() {
        InitializeComponent();
        visualizables = Visualizables.Database;
        waveform.ItemsSource = visualizables;
    }

    /// <summary>
    /// When the configuration has changed, redraw the waveform.
    /// </summary>
    void Redraw(object _, RoutedEventArgs e) {
        if (waveform.SelectedIndex == -1) {
            return;
        }

        float[] result = visualizables[waveform.SelectedIndex].Produce(audioFile);
        samples.Text = "Samples: " + result.Length;

        float peak = (normalize.IsChecked.Value ? WaveformUtils.GetPeak(result) : 1) * (float)scale;
        WaveformRenderer renderer = new((int)imageHolder.ActualWidth, (int)imageHolder.ActualHeight) {
            Peak = peak,
            DynamicRange = peak * 2,
            Overlay = new Cavern.QuickEQ.Graphing.Overlays.Grid(2, 1, 0xFF000000, 0, 1),
        };

        renderer.AddWaveform(result, 0xFF0000FF);
        image.Source = renderer.ToBitmapSource();
    }

    /// <summary>
    /// Load a reference file and enable file-dependent visualizations.
    /// </summary>
    void LoadFile(object _, RoutedEventArgs e) {
        OpenFileDialog dialog = new() {
            Filter = "Supported audio files|" + AudioReader.filter
        };
        if (!dialog.ShowDialog().Value) {
            return;
        }

        if (audioFile == null) {
            visualizables = Visualizables.All;
            waveform.ItemsSource = visualizables;
        }

        using AudioReader reader = AudioReader.Open(dialog.FileName);
        audioFile = reader.ReadMultichannel()[0]; // Only the first channel is used
        sampleRate = reader.SampleRate;
        waveform.SelectedItem = Visualizables.rawFile;
        Redraw(null, null);
    }

    /// <summary>
    /// Export the currently displayed waveform as a WAV file in float32 format.
    /// </summary>
    void ExportDisplayed(object _, RoutedEventArgs e) {
        if (waveform.SelectedIndex == -1) {
            return;
        }

        float[] result = visualizables[waveform.SelectedIndex].Produce(audioFile);
        if (result == null || result.Length == 0) {
            return;
        }

        SaveFileDialog dialog = new() {
            Filter = "WAV files (*.wav)|*.wav",
            DefaultExt = ".wav",
            FileName = "Displayed.wav"
        };

        if (!dialog.ShowDialog().Value) {
            return;
        }

        AudioWriter.Write(dialog.FileName, new MultichannelWaveform([result]), sampleRate, BitDepth.Float32);
    }

    /// <summary>
    /// When the window is resized, adapt the image.
    /// </summary>
    void OnResize(object _, SizeChangedEventArgs e) {
        if (e.NewSize.Width != 0 && e.NewSize.Height != 0) {
            Redraw(null, null);
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

        Redraw(null, null);
    }

    /// <summary>
    /// The user has selected a new waveform for display.
    /// </summary>
    void OnWaveformChanged(object _, SelectionChangedEventArgs e) {
        scale = 1; // Reset zoom
        Redraw(null, null);
    }

    /// <summary>
    /// How much to change when scrolling
    /// </summary>
    const double scaleFactor = 1.25;
}
