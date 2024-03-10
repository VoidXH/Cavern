using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;

using Cavern;
using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format;
using Cavern.WPF;

using Color = System.Drawing.Color;

namespace WavefrontSimulator {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        class Simulated {
            Vector2 position;
            readonly float[] samples;

            public Vector2 Position { get; private set; }
            public Vector2 Direction { get; private set; }
            public float Distance { get; private set; }
            public float[] Samples { get; private set; }
            public int SampleCount => samples.Length;

            public Simulated(Channel channel, float[] samples) {
                Vector3 cubicalPos = channel.CubicalPos;
                position = new(cubicalPos.X, cubicalPos.Z);
                this.samples = samples;
                Position = new();
                float min = Math.Min(Math.Abs(position.X), Math.Abs(position.Y));
                if (min == Math.Abs(position.X)) {
                    Direction = new Vector2(0, Math.Sign(-position.Y));
                } else {
                    Direction = new Vector2(Math.Sign(-position.X), 0);
                }
                Distance = 0;
                Samples = null;
            }

            public Simulated WorkWith(float roomSize, float[] samples) {
                Position = position * roomSize;
                Distance = Position.Length();
                Samples = FastConvolver.Convolve(this.samples, samples);
                return this;
            }
        }

        readonly List<Simulated> data = [];
        int sampleRate;

        float wallLength = 5;
        float directivityIndex = 3;

        public MainWindow() {
            InitializeComponent();
            toeIn.IsChecked = Settings.Default.toeIn;
            realTime.IsChecked = Settings.Default.realTime;
            dirIndexValue.Value = Settings.Default.dirIndex;
            wallLenValue.Value = Settings.Default.wallLen;
        }

        void LoadImpulseResponse(object _, RoutedEventArgs e) {
            OpenFileDialog dialog = new() {
                Filter = "RIFF WAVE files (*.wav)|*.wav"
            };
            if (dialog.ShowDialog() == true) {
                AudioReader reader = AudioReader.Open(dialog.FileName);
                float[] samples = reader.Read();
                int cut1 = dialog.FileName.IndexOf('_'), cut2 = dialog.FileName.IndexOf('.');
                string channelName = dialog.FileName[(cut1 + 1)..cut2];
                ChannelPrototype channel = ChannelPrototype.FromStandardName(channelName);
                data.Add(new Simulated(new Channel(channel.X, channel.Y), samples));
                sampleRate = reader.SampleRate;
                clear.IsEnabled = true;
            }
        }

        void Clear(object _, RoutedEventArgs e) {
            data.Clear();
            clear.IsEnabled = false;
        }

        void DirectivityIndexChanged(object sender, RoutedEventArgs _) {
            directivityIndex = (float)Math.Round(((Slider)sender).Value * 10) * .1f;
            if (dirIndex != null) {
                dirIndex.Text = directivityIndex.ToString("0.0");
            }
            if (realTime.IsChecked.Value && data.Count != 0) {
                Render(null, null);
            }
        }

        void WallLengthChanged(object sender, RoutedEventArgs _) {
            wallLength = (float)Math.Round(((Slider)sender).Value * 10) * .1f;
            if (wallLen != null) {
                wallLen.Text = wallLength.ToString("0.0 m");
            }
            if (realTime.IsChecked.Value && data.Count != 0) {
                Render(null, null);
            }
        }

        static Color DrawPixel(float strength) {
            if (!float.IsNaN(strength)) {
                return Color.FromArgb(
                    Math.Max((int)(255 * (1 - Math.Abs(2 * strength))), 0),
                    (int)(127 * 1 - Math.Abs(2 * (strength - .5f))),
                    Math.Max((int)(255 * (1 - Math.Abs(2 * (strength - 1)))), 0)
                );
            } else {
                return Color.Black;
            }
        }

        void Render(object _, RoutedEventArgs e) {
            if (data.Count == 0) {
                MessageBox.Show("No impulse responses were imported.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            float[] sine = new float[data[0].SampleCount];
            const float frequency = 1000;
            for (int i = 0; i < sine.Length; ++i) {
                sine[i] = MathF.Sin(2 * MathF.PI * frequency * i / sampleRate);
            }
            float roomSizeUnit = wallLength / 2;
            for (int i = 0; i < data.Count; ++i) {
                data[i] = data[i].WorkWith(roomSizeUnit, sine);
            }

            int size = (int)image.Width;
            float[,] gains = new float[size, size];
            float maxGain = 0, minGain = float.PositiveInfinity;
            for (int x = 0; x < size; ++x) {
                for (int y = 0; y < size; ++y) {
                    Vector2 dotPos = new Vector2(x, y) * new Vector2(wallLength / size) - new Vector2(roomSizeUnit);
                    float result = 0;
                    for (int i = 0; i < data.Count; ++i) {
                        Vector2 distance = dotPos - data[i].Position;
                        int delay = (int)(distance.Length() / Source.SpeedOfSound * sampleRate);
                        float dot = toeIn.IsChecked.Value ?
                            Vector2.Dot(-data[i].Position / data[i].Distance, distance / distance.Length()) :
                            Vector2.Dot(data[i].Direction, distance / distance.Length());
                        float directivity = MathF.Acos(MathF.Sign(dot) - MathF.Pow(dot, directivityIndex));
                        result += data[i].Samples[delay] * directivity;
                    }
                    gains[x, y] = result;
                    if (maxGain < gains[x, y]) {
                        maxGain = gains[x, y];
                    }
                    if (minGain > gains[x, y]) {
                        minGain = gains[x, y];
                    }
                }
            }

            float uniformityValue = 0, center = (maxGain - minGain) * .5f, middle = minGain + center;
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    float addition = MathF.Abs(gains[x, y] - middle);
                    if (!float.IsNaN(addition)) {
                        uniformityValue += addition;
                    }
                }
            }
            uniformityValue = 1 - (uniformityValue / (size * size * center));
            uniformity.Text = "Uniformity: " + uniformityValue.ToString("0.00%");

            Bitmap output = new Bitmap(size, size);
            maxGain -= minGain;
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    float pxval = (gains[x, y] - minGain) / maxGain;
                    output.SetPixel(x, size - y - 1, DrawPixel(pxval));
                }
            }
            image.Tag = output;
            image.Source = BitmapUtils.ToImageSource(output);
        }

        void ExportRender(object sender, RoutedEventArgs e) {
            if (image.Tag == null) {
                MessageBox.Show("Please render an image first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveFileDialog dialog = new() {
                FileName = $"Waveform simulation - {uniformity.Text.Replace(":", " -").Replace(',', '.')}.bmp",
                Filter = "Bitmap files (*.bmp)|*.bmp"
            };
            if (dialog.ShowDialog() == true) {
                ((Bitmap)image.Tag).Save(dialog.FileName);
            }
        }

        void OnExit(object _, CancelEventArgs e) {
            Settings.Default.toeIn = toeIn.IsChecked.Value;
            Settings.Default.realTime = realTime.IsChecked.Value;
            Settings.Default.dirIndex = (float)dirIndexValue.Value;
            Settings.Default.wallLen = (float)wallLenValue.Value;
            Settings.Default.Save();
        }
    }
}