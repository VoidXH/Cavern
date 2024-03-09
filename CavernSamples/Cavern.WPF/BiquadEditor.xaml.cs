using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cavern.Filters;
using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Graphing;
using Cavern.QuickEQ.Graphing.Overlays;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

using Color = System.Windows.Media.Color;

namespace Cavern.WPF {
    /// <summary>
    /// Biquad filter customization/editor window.
    /// </summary>
    public partial class BiquadEditor : Window {
        /// <summary>
        /// The filter created by the user's inputs.
        /// </summary>
        public BiquadFilter Filter { get; private set; }

        /// <summary>
        /// Draws the displayed spectrum/phase graph.
        /// </summary>
        readonly GraphRenderer renderer;

        /// <summary>
        /// Biquad filter customization/editor window.
        /// </summary>
        public BiquadEditor() {
            InitializeComponent();
            renderer = new GraphRenderer((int)(image.Width + .5), (int)(image.Height + .5)) {
                DynamicRange = 50,
                Peak = 25,
                Overlay = new LogScaleGrid(2, 1, 0xFF000000, 10)
            };
            filterTypes.ItemsSource = Enum.GetValues(typeof(BiquadFilterType));
            filterTypes.SelectedIndex = 0;
        }

        /// <summary>
        /// Re-render the image when the filter type is changed and only enable parameters that are interpreted for said filter.
        /// </summary>
        void FilterTypeChanged(object _, SelectionChangedEventArgs e) {
            RecreateFilter();
            swapPhase.IsEnabled = Filter is PhaseSwappableBiquadFilter;
        }

        /// <summary>
        /// Re-render the image when a numeric value is changed, and paint <see cref="TextBox"/>es red when their value is invalid and
        /// preventing the <see cref="Filter"/> from being recreated.
        /// </summary>
        void NumericFilterDataChanged(object sender, TextChangedEventArgs e) {
            Color color = RecreateFilter() ? Color.FromRgb(255, 255, 255) : Color.FromRgb(255, 127, 127);
            ((TextBox)sender).Background = new SolidColorBrush(color);
        }

        /// <summary>
        /// Re-render the image when the phase of the filter is swapped.
        /// </summary>
        void PhaseSwapChanged(object _, RoutedEventArgs e) => RecreateFilter();

        /// <summary>
        /// Use all entered data to update the selected <see cref="Filter"/>.
        /// </summary>
        bool RecreateFilter() {
            if (this.centerFreq == null || this.q == null || this.gain == null || filterTypes.SelectedItem == null) {
                return true; // Initializing, no need for checks
            }

            if (double.TryParse(this.centerFreq.Text, CultureInfo.InvariantCulture, out double centerFreq) &&
                double.TryParse(this.q.Text, CultureInfo.InvariantCulture, out double q) &&
                double.TryParse(this.gain.Text, CultureInfo.InvariantCulture, out double gain)) {
                Filter = BiquadFilter.Create((BiquadFilterType)filterTypes.SelectedItem, 48000, centerFreq, q, gain);
                if (Filter is PhaseSwappableBiquadFilter swappable) {
                    swappable.PhaseSwapped = swapPhase.IsChecked.Value;
                }
                Draw();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Re-render the spectrum and phase diagram after changes.
        /// </summary>
        void Draw() {
            renderer.Clear();
            FilterAnalyzer analyzer = new FilterAnalyzer(Filter, Filter.SampleRate);
            Complex[] transferFunction = analyzer.GetFrequencyResponse();
            renderer.AddCurve(EQGenerator.FromTransferFunction(transferFunction, Filter.SampleRate), 0xFF0000FF);

            for (int i = 0; i < transferFunction.Length; i++) {
                // Phase hacked into gain, -25 dB = -pi, 25 dB = pi
                transferFunction[i] = new Complex(QMath.DbToGain(transferFunction[i].Phase * 25 / MathF.PI));
            }
            renderer.AddCurve(EQGenerator.FromTransferFunction(transferFunction, Filter.SampleRate), 0xFF00FF00);

            Bitmap bitmap = renderer.Pixels.ToBitmap(renderer.Width, renderer.Height);
            image.Source = BitmapUtils.ToImageSource(bitmap);
        }
    }
}