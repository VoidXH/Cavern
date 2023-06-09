using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;

using Cavern.Format;
using Cavern.Utilities;

using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace HRTFSetImporter {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        const string hMarker = "{Y}", wMarker = "{X}", angleMarker = "{A}", distanceMarker = "{D}";

        readonly FolderBrowserDialog importer = new FolderBrowserDialog();

        public MainWindow() {
            InitializeComponent();
            if (!string.IsNullOrEmpty(Settings.Default.LastFolder) && Directory.Exists(Settings.Default.LastFolder)) {
                importer.SelectedPath = Settings.Default.LastFolder;
            }
            directionalSetName.Text = Settings.Default.DirectionalSetName;
            angleSetName.Text = Settings.Default.AngleSetName;
            useSpaces.IsChecked = Settings.Default.UseSpaces;
        }

        static Dictionary<int, Dictionary<int, float[][]>> ImportImpulses(string path, Regex pattern) {
            Settings.Default.LastFolder = path;
            string[] files = Directory.GetFiles(path);
            Dictionary<int, Dictionary<int, float[][]>> data = new Dictionary<int, Dictionary<int, float[][]>>();
            for (int file = 0; file < files.Length; ++file) {
                string fileName = Path.GetFileName(files[file]);
                Match match = pattern.Match(fileName);
                if (match.Success &&
                    int.TryParse(match.Groups["param1"].Value, out int angle) &&
                    int.TryParse(match.Groups["param2"].Value, out int distance)) {
                    if (!data.ContainsKey(angle)) {
                        data.Add(angle, new Dictionary<int, float[][]>());
                    }
                    data[angle][distance] = AudioReader.Open(files[file]).ReadMultichannel();
                }
            }
            return data;
        }

        static bool WriteDirectionalChannel(StringBuilder builder, List<int> written, int h, int w, float[][] samples) {
            int hash = h * 1000 + w;
            if (written.Contains(hash)) {
                return false;
            }
            written.Add(hash);
            int hValue = -h;
            if (hValue <= -180) {
                hValue += 360;
            }
            builder.AppendLine("\tnew VirtualChannel(").Append(-w).Append(", ").Append(hValue).Append(",\n")
                .Append("\t\t").AppendArray(samples[0])
                .Append("\t\t").AppendArray(samples[1])
                .AppendLine("\t},");
            return true;
        }

        static void LeadingClearing(Dictionary<int, Dictionary<int, float[][]>> data) {
            int minLead = int.MaxValue;
            foreach (KeyValuePair<int, Dictionary<int, float[][]>> angle in data) {
                foreach (KeyValuePair<int, float[][]> distance in angle.Value) {
                    float[] samples = distance.Value[0];
                    int zeros = 0;
                    while (zeros < samples.Length && samples[zeros] == 0) {
                        ++zeros;
                    }
                    if (minLead > zeros) {
                        minLead = zeros;
                    }
                }
            }

            int[] angles = data.Keys.ToArray();
            foreach (int angle in angles) {
                int[] distances = data[angle].Keys.ToArray();
                foreach (int distance in distances) {
                    float[] samples = data[angle][distance][0];
                    int newSize = samples.Length - minLead;
                    for (int i = 0; i < newSize; ++i) {
                        samples[i] = samples[i + minLead];
                    }
                    Array.Resize(ref samples, newSize);
                    data[angle][distance][0] = samples;
                }
            }
        }

        static void Normalize(Dictionary<int, Dictionary<int, float[][]>> data) {
            foreach (KeyValuePair<int, Dictionary<int, float[][]>> angle in data) {
                foreach (KeyValuePair<int, float[][]> distance in angle.Value) {
                    distance.Value[0].Normalize();
                }
            }
        }

        static void TrailingClearing(Dictionary<int, Dictionary<int, float[][]>> data) {
            int[] angles = data.Keys.ToArray();
            foreach (int angle in angles) {
                int[] distances = data[angle].Keys.ToArray();
                foreach (int distance in distances) {
                    float[] samples = data[angle][distance][0];
                    int clearUntil = samples.Length - 1;
                    while (clearUntil >= 0 && samples[clearUntil] == 0) {
                        --clearUntil;
                    }
                    Array.Resize(ref samples, clearUntil + 1);
                    data[angle][distance][0] = samples;
                }
            }
        }

        /// <summary>
        /// Copy the <paramref name="result"/> to the clipboard in the expected format.
        /// </summary>
        void Finalize(StringBuilder result) {
            if (useSpaces.IsChecked.Value) {
                result.Replace("\t", "    ");
            }
            Clipboard.SetText(result.ToString());
            MessageBox.Show("Impulse response array successfully copied to clipboard.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void ImportAngleSet(object _, RoutedEventArgs e) {
            if (importer.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Dictionary<int, Dictionary<int, float[][]>> data = ImportSet(angleSetName.Name, angleMarker, distanceMarker);
                if (data == null) {
                    return;
                }

                LeadingClearing(data);
                TrailingClearing(data);
                Normalize(data);

                StringBuilder result = new StringBuilder("static readonly float[][][] impulses = new float[")
                    .Append(data.Count).AppendLine("][][] {");
                IOrderedEnumerable<KeyValuePair<int, Dictionary<int, float[][]>>> orderedData = data.OrderBy(entry => entry.Key);
                foreach (KeyValuePair<int, Dictionary<int, float[][]>> angle in orderedData) {
                    result.Append("\tnew float[").Append(angle.Value.Count).AppendLine("][] {");
                    IOrderedEnumerable<KeyValuePair<int, float[][]>> orderedDistances = angle.Value.OrderBy(entry => entry.Key);
                    foreach (KeyValuePair<int, float[][]> distance in orderedDistances) {
                        result.Append("\t\t").AppendArray(distance.Value[0]);
                    }
                    result.AppendLine("\t},");
                }
                result.Append("};");
                Finalize(result);
            }
        }

        void ImportDirectionalSet(object _, EventArgs e) {
            if (importer.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Dictionary<int, Dictionary<int, float[][]>> data = ImportSet(directionalSetName.Text, hMarker, wMarker);
                if (data == null) {
                    return;
                }

                List<int> written = new List<int>(); // Already exported channels' hash
                StringBuilder result = new StringBuilder()
                    .AppendLine("static VirtualChannel[] GetDefaultHRIRSet() => new[] {");
                IOrderedEnumerable<KeyValuePair<int, Dictionary<int, float[][]>>> orderedH = data.OrderBy(entry => entry.Key);
                foreach (KeyValuePair<int, Dictionary<int, float[][]>> hPoint in orderedH) {
                    IOrderedEnumerable<KeyValuePair<int, float[][]>> orderedW = hPoint.Value.OrderBy(entry => entry.Key);
                    foreach (KeyValuePair<int, float[][]> wPoint in orderedW) {
                        if (!WriteDirectionalChannel(result, written, hPoint.Key, wPoint.Key, wPoint.Value)) {
                            continue;
                        }
                        int pair = 360 - hPoint.Key;
                        if (data.ContainsKey(pair) && data[pair].ContainsKey(wPoint.Key)) {
                            WriteDirectionalChannel(result, written, pair, wPoint.Key, data[pair][wPoint.Key]);
                        }
                    }
                }
                result.Append("};");
                Finalize(result);
            }
        }

        /// <summary>
        /// Import a set of HRTF files conforming to a 2-parameter file name format.
        /// </summary>
        Dictionary<int, Dictionary<int, float[][]>> ImportSet(string setName, string param1Marker, string param2Marker) {
            string format = setName
                    .Replace(param1Marker, "(?<param1>.+)")
                    .Replace(param2Marker, "(?<param2>.+)");
            Regex pattern = new Regex(format);

            Dictionary<int, Dictionary<int, float[][]>> data = ImportImpulses(importer.SelectedPath, pattern);
            if (data.Count == 0) {
                MessageBox.Show("No files were found in the selected folder matching the given file name format.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            if (data.First().Value.First().Value.Length != 2) {
                MessageBox.Show("Only stereo directional files are supported.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            return data;
        }

        protected override void OnClosed(EventArgs e) {
            Settings.Default.DirectionalSetName = directionalSetName.Text;
            Settings.Default.AngleSetName = angleSetName.Text;
            Settings.Default.UseSpaces = useSpaces.IsChecked.Value;
            Settings.Default.Save();
            base.OnClosed(e);
        }
    }
}