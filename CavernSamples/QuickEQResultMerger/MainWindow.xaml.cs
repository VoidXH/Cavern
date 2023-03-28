using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace QuickEQResultMerger {
    /// <summary>
    /// Interaction logic for the main window.
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// Gain/delay collection picker.
        /// </summary>
        readonly OpenFileDialog dialog = new() {
            Filter = "Text files (*.txt)|*.txt"
        };

        /// <summary>
        /// Display of merged results.
        /// </summary>
        readonly ObservableCollection<Measurement> results = new();

        /// <summary>
        /// Create the window.
        /// </summary>
        public MainWindow() => InitializeComponent();

        /// <summary>
        /// Open a file and automatically merge with everything that was already open.
        /// </summary>
        void AddFile(object _, RoutedEventArgs e) {
            if (dialog.ShowDialog().Value) {
                ResultFile file = new(dialog.FileName);
                Merge(file.measurements);
                CorrectDelays();
            }
        }

        /// <summary>
        /// Merge new measurements with the past ones.
        /// </summary>
        void Merge(List<Measurement> measurements) {
            Measurement newCommon = null, oldCommon = null;
            for (int i = 0; i < measurements.Count && newCommon == null; ++i) {
                for (int j = 0; j < results.Count; ++j) {
                    if (measurements[i].Channel.Equals(results[j].Channel)) {
                        newCommon = measurements[i];
                        oldCommon = results[j];
                        break;
                    }
                }
            }

            if (newCommon != null) {
                float gainChange = newCommon.Gain - oldCommon.Gain;
                float delayChange = newCommon.Delay - oldCommon.Delay;
                for (int i = 0; i < measurements.Count; ++i) {
                    measurements[i].Correct(gainChange, delayChange);
                }
            }

            for (int i = 0; i < measurements.Count; ++i) {
                bool has = false;
                for (int j = 0; j < results.Count; ++j) {
                    if (results[j].Channel.Equals(measurements[i].Channel)) {
                        has = true;
                        break;
                    }
                }
                if (!has) {
                    results.Add(measurements[i]);
                }
            }
        }

        /// <summary>
        /// Makes sure there are no negative delays.
        /// </summary>
        void CorrectDelays() {
            float min = results.Min().Delay;
            if (min < 0) {
                for (int i = 0; i < results.Count; ++i) {
                    results[i].Correct(0, min);
                }

                // Update the observable collection by force
                List<Measurement> cache = new();
                cache.AddRange(results);
                results.Clear();
                for (int i = 0; i < cache.Count; ++i) {
                    results.Add(cache[i]);
                }
            }
            files.ItemsSource = results;
        }

        /// <summary>
        /// Start with a clean sheet.
        /// </summary>
        void Clear(object _, RoutedEventArgs e) => results.Clear();
    }
}