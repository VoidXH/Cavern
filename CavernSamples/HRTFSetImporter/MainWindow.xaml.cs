using Cavern.Format;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;

using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace HRTFSetImporter {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        const string angleMarker = "{A}", distanceMarker = "{D}";

        readonly FolderBrowserDialog importer = new FolderBrowserDialog();
        readonly NumberFormatInfo numberFormat = new NumberFormatInfo {
            NumberDecimalSeparator = "."
        };

        public MainWindow() => InitializeComponent();

        Dictionary<int, Dictionary<int, float[]>> ImportImpulses(string path, Regex pattern) {
            string[] folders = Directory.GetFiles(importer.SelectedPath);
            Dictionary<int, Dictionary<int, float[]>> data = new Dictionary<int, Dictionary<int, float[]>>(); // angle, distance
            for (int file = 0; file < folders.Length; ++file) {
                string fileName = Path.GetFileName(folders[file]);
                Match match = pattern.Match(fileName);
                if (match.Success &&
                    int.TryParse(match.Groups["angle"].Value, out int angle) &&
                    int.TryParse(match.Groups["distance"].Value, out int distance)) {
                    if (!data.ContainsKey(angle))
                        data.Add(angle, new Dictionary<int, float[]>());
                    RIFFWaveReader reader = new RIFFWaveReader(new BinaryReader(File.OpenRead(folders[file])));
                    data[angle][distance] = reader.Read();
                }
            }
            return data;
        }

        void LeadingClearing(Dictionary<int, Dictionary<int, float[]>> data) {
            int minLead = int.MaxValue;
            foreach (KeyValuePair<int, Dictionary<int, float[]>> angle in data) {
                foreach (KeyValuePair<int, float[]> distance in angle.Value) {
                    float[] samples = distance.Value;
                    int zeros = 0;
                    while (zeros < samples.Length && samples[zeros] == 0)
                        ++zeros;
                    if (minLead > zeros)
                        minLead = zeros;
                }
            }

            int[] angles = data.Keys.ToArray();
            foreach (int angle in angles) {
                int[] distances = data[angle].Keys.ToArray();
                foreach (int distance in distances) {
                    float[] samples = data[angle][distance];
                    int newSize = samples.Length - minLead;
                    for (int i = 0; i < newSize; ++i)
                        samples[i] = samples[i + minLead];
                    Array.Resize(ref samples, newSize);
                    data[angle][distance] = samples;
                }
            }
        }

        void TrailingClearing(Dictionary<int, Dictionary<int, float[]>> data) {
            int[] angles = data.Keys.ToArray();
            foreach (int angle in angles) {
                int[] distances = data[angle].Keys.ToArray();
                foreach (int distance in distances) {
                    float[] samples = data[angle][distance];
                    int clearUntil = samples.Length - 1;
                    while (clearUntil >= 0 && samples[clearUntil] == 0)
                        --clearUntil;
                    Array.Resize(ref samples, clearUntil + 1);
                    data[angle][distance] = samples;
                }
            }
        }

        void ImportAngleSet(object sender, RoutedEventArgs e) {
            if (importer.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                string format = angleSetName.Text
                    .Replace(angleMarker, "(?<angle>.+)")
                    .Replace(distanceMarker, "(?<distance>.+)");
                Regex pattern = new Regex(format);

                Dictionary<int, Dictionary<int, float[]>> data = ImportImpulses(importer.SelectedPath, pattern);
                if (data.Count == 0) {
                    MessageBox.Show("No files were found in the selected folder matching the given file name format.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LeadingClearing(data);
                TrailingClearing(data);

                StringBuilder result = new StringBuilder("float[][][] impulses = new float[").Append(data.Count).AppendLine("][][] {");
                IOrderedEnumerable<KeyValuePair<int, Dictionary<int, float[]>>> orderedData = data.OrderBy(entry => entry.Key);
                foreach (KeyValuePair<int, Dictionary<int, float[]>> angle in orderedData) {
                    result.Append("\tnew float[").Append(angle.Value.Count).AppendLine("][] {");
                    IOrderedEnumerable<KeyValuePair<int, float[]>> orderedDistances = angle.Value.OrderBy(entry => entry.Key);
                    foreach (KeyValuePair<int, float[]> distance in orderedDistances) {
                        float[] samples = distance.Value;
                        result.Append("\t\tnew float[").Append(samples.Length).Append("] { ");
                        for (int i = 0; i < samples.Length; ++i)
                            result.Append(samples[i].ToString(numberFormat)).Append("f, ");
                        result.Remove(result.Length - 2, 2).AppendLine(" },");
                    }
                    result.AppendLine("\t},");
                }
                result.Append("};");
                Clipboard.SetText(result.ToString());
                MessageBox.Show("Impulse response array successfully copied to clipboard.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}