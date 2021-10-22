using Cavern.Utilities;
using HRTFSetStatista.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;

using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace HRTFSetStatista {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        const string hMarker = "{Y}", wMarker = "{X}", distanceMarker = "{D}";
        /// <summary>
        /// -20 dB gain, 10^(-20/20).
        /// </summary>
        const float m20dB = .1f;

        readonly FolderBrowserDialog importer = new FolderBrowserDialog();
        readonly NumberFormatInfo numberFormat = new NumberFormatInfo {
            NumberDecimalSeparator = "."
        };

        public MainWindow() {
            InitializeComponent();
            if (Settings.Default.LastPath != null && Directory.Exists(Settings.Default.LastPath))
                importer.SelectedPath = Settings.Default.LastPath;
            setName.Text = Settings.Default.SetName;
        }

        void Error(string error) => MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        Dictionary<double, int[]> GetDelayDifferences(List<HRTFSetEntry> entries, List<double> hValues) {
            Dictionary<double, int[]> result = new Dictionary<double, int[]>(); // Distance; array by hValues
            for (int entry = 0, count = entries.Count; entry < count; ++entry) {
                HRTFSetEntry setEntry = entries[entry];
                int maxDelay = 0, minDelay = int.MaxValue;
                for (int channel = 0; channel < setEntry.Data.Length; ++channel) {
                    float[] data = setEntry.Data[channel];
                    int delay = 0;
                    float firstValid = WaveformUtils.GetPeak(data) * m20dB;
                    while (delay < data.Length && Math.Abs(data[delay]) < firstValid)
                        ++delay;
                    if (maxDelay < delay)
                        maxDelay = delay;
                    if (minDelay > delay)
                        minDelay = delay;
                }
                if (!result.ContainsKey(setEntry.Distance))
                    result[setEntry.Distance] = new int[hValues.Count];
                result[setEntry.Distance][hValues.IndexOf(setEntry.Azimuth)] = maxDelay - minDelay;
            }
            return result;
        }

        Dictionary<double, float[]> GetGainDifferences(List<HRTFSetEntry> entries, List<double> hValues) {
            Dictionary<double, float[]> result = new Dictionary<double, float[]>(); // Distance; array by hValues
            for (int entry = 0, count = entries.Count; entry < count; ++entry) {
                HRTFSetEntry setEntry = entries[entry];
                float maxGain = float.MinValue, minGain = float.MaxValue;
                for (int channel = 0; channel < setEntry.Data.Length; ++channel) {
                    float peak = QMath.GainToDb(WaveformUtils.GetRMS(setEntry.Data[channel]));
                    if (maxGain < peak)
                        maxGain = peak;
                    if (minGain > peak)
                        minGain = peak;
                }
                if (!result.ContainsKey(setEntry.Distance))
                    result[setEntry.Distance] = new float[hValues.Count];
                result[setEntry.Distance][hValues.IndexOf(setEntry.Azimuth)] = maxGain - minGain;
            }
            return result;
        }

        void CopyToClipboard<T>(Dictionary<double, T[]> set, List<double> hValues) {
            StringBuilder result = new StringBuilder();
            double[] columns = set.Keys.ToArray();
            for (int h = 0; h < hValues.Count; ++h) {
                result.Append(hValues[h]).Append('\t');
                for (int i = 0; i < columns.Length; ++i)
                    if (i != columns.Length - 1)
                        result.Append(set[columns[i]][h].ToString()).Append('\t');
                    else
                        result.AppendLine(set[columns[i]][h].ToString());
            }
            Clipboard.SetText(result.ToString());
        }

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
                    if (match.Success) {
                        if (!double.TryParse(match.Groups["h"].Value, NumberStyles.Any, numberFormat, out double h))
                            continue;
                        double.TryParse(match.Groups["w"].Value, NumberStyles.Any, numberFormat, out double w);
                        if (!double.TryParse(match.Groups["distance"].Value, NumberStyles.Any, numberFormat, out double distance))
                            distance = 1;
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

                Dictionary<double, int[]> delayDiffs = GetDelayDifferences(entries, hValues);
                Dictionary<double, float[]> gainDiffs = GetGainDifferences(entries, hValues);

                // TODO: draw graphs
            }
        }

        protected override void OnClosing(CancelEventArgs e) {
            Settings.Default.LastPath = importer.SelectedPath;
            Settings.Default.SetName = setName.Text;
            Settings.Default.Save();
            base.OnClosing(e);
        }
    }
}