using Cavern.QuickEQ;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;

using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace HRTFSetStatista {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        const string hMarker = "{Y}", wMarker = "{X}", distanceMarker = "{D}";

        readonly FolderBrowserDialog importer = new FolderBrowserDialog();
        readonly NumberFormatInfo numberFormat = new NumberFormatInfo {
            NumberDecimalSeparator = "."
        };

        public MainWindow() => InitializeComponent();

        void Error(string error) => MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        void LoadSet(object sender, RoutedEventArgs e) {
            if (importer.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                string format = setName.Text
                    .Replace(hMarker, "(?<h>.+)")
                    .Replace(wMarker, "(?<w>.+)")
                    .Replace(distanceMarker, "(?<distance>.+)");
                Regex pattern = new Regex(format);
                string[] folders = Directory.GetFiles(importer.SelectedPath);
                List<HRTFSetEntry> entries = new List<HRTFSetEntry>();
                for (int file = 0; file < folders.Length; ++file) {
                    string fileName = Path.GetFileName(folders[file]);
                    Match match = pattern.Match(fileName);
                    if (match.Success &&
                        double.TryParse(match.Groups["h"].Value, NumberStyles.Any, numberFormat, out double h) &&
                        double.TryParse(match.Groups["w"].Value, NumberStyles.Any, numberFormat, out double w) &&
                        double.TryParse(match.Groups["distance"].Value, NumberStyles.Any, numberFormat, out double distance)) {
                        entries.Add(new HRTFSetEntry(h, w, distance, folders[file]));
                    }
                }
                int entryCount = entries.Count;
                if (entryCount == 0) {
                    Error($"No files matched \"{setName.Text}\" in the selected folder ({Path.GetFileName(importer.SelectedPath)}.");
                    return;
                }

                entries = entries.OrderBy(entry => entry.Azimuth + entry.Elevation * .001 + entry.Distance * .00001).ToList();
                List<double> hValues = new List<double> {
                    entries[0].Azimuth
                };
                for (int entry = 0; entry < entryCount; ++entry)
                    if (!hValues.Contains(entries[entry].Azimuth))
                        hValues.Add(entries[entry].Azimuth);

                Dictionary<double, int[]> delayDiffs = new Dictionary<double, int[]>(); // Distance; array by hValues
                for (int entry = 0; entry < entryCount; ++entry) {
                    HRTFSetEntry setEntry = entries[entry];
                    int maxDelay = 0, minDelay = int.MaxValue;
                    for (int channel = 0; channel < setEntry.Data.Length; ++channel) {
                        VerboseImpulseResponse response = new VerboseImpulseResponse(setEntry.Data[channel]);
                        if (maxDelay < response.Delay)
                            maxDelay = response.Delay;
                        if (minDelay > response.Delay)
                            minDelay = response.Delay;
                    }
                    if (!delayDiffs.ContainsKey(setEntry.Distance))
                        delayDiffs[setEntry.Distance] = new int[hValues.Count];
                    delayDiffs[setEntry.Distance][hValues.IndexOf(setEntry.Azimuth)] = maxDelay - minDelay;
                }

                // TODO: draw the delayDiffs graphs by each distance
            }
        }
    }
}