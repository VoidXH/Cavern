using Cavern;
using Cavern.Filters;
using Cavern.Format;
using Cavern.Remapping;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;

namespace WavefrontSimulator {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        struct Simulated {
            Vector2 position;
            float[] samples;

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
                if (min == Math.Abs(position.X))
                    Direction = new Vector2(0, Math.Sign(-position.Y));
                else
                    Direction = new Vector2(Math.Sign(-position.X), 0);
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

        readonly List<Simulated> data = new();
        int sampleRate;

        const float roomSize = 5; // meters, TODO: slider
        const float directivityIndex = 3; // TODO: slider
        // TODO: calculate uniformity, export image

        public MainWindow() => InitializeComponent();

        void LoadImpulseResponse(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new() {
                Filter = "RIFF WAVE files (*.wav)|*.wav"
            };
            if (dialog.ShowDialog() == true) {
                RIFFWaveReader reader = new RIFFWaveReader(dialog.FileName);
                float[] samples = reader.Read();
                int cut1 = dialog.FileName.IndexOf('_'), cut2 = dialog.FileName.IndexOf('.');
                string channelName = dialog.FileName[(cut1 + 1)..cut2];
                ChannelPrototype channel = ChannelPrototype.FromStandardName(channelName);
                data.Add(new Simulated(new Channel(channel.X, channel.Y), samples));
                sampleRate = reader.SampleRate;
            }
        }

        static Color DrawPixel(float strength) {
            return Color.FromArgb(
                Math.Max((int)(255 * (1 - Math.Abs(2 * strength))), 0),
                (int)(127 * 1 - Math.Abs(2 * (strength - .5f))),
                Math.Max((int)(255 * (1 - Math.Abs(2 * (strength - 1)))), 0)
            );
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteObject([In] IntPtr hObject);

        static ImageSource ToImageSource(Bitmap bmp) {
            IntPtr handle = bmp.GetHbitmap();
            try {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            } finally {
                DeleteObject(handle);
            }
        }

        void Render(object sender, RoutedEventArgs e) {
            // TODO: was anything loaded
            float[] sine = new float[data[0].SampleCount];
            const float frequency = 1000;
            for (int i = 0; i < sine.Length; ++i)
                sine[i] = MathF.Sin(2 * MathF.PI * frequency * i / sampleRate);
            float roomSizeUnit = roomSize / 2;
            for (int i = 0; i < data.Count; ++i)
                data[i] = data[i].WorkWith(roomSizeUnit, sine);

            int size = (int)image.Width;
            float[,] gains = new float[size, size];
            float maxGain = 0, minGain = float.PositiveInfinity;
            for (int x = 0; x < size; ++x) {
                for (int y = 0; y < size; ++y) {
                    Vector2 dotPos = new Vector2(x, y) * new Vector2(roomSize / size) - new Vector2(roomSizeUnit);
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
                    if (maxGain < gains[x, y])
                        maxGain = gains[x, y];
                    if (minGain > gains[x, y])
                        minGain = gains[x, y];
                }
            }
            
            Bitmap output = new Bitmap(size, size);
            maxGain -= minGain;
            for (int x = 0; x < size; ++x) {
                for (int y = 0; y < size; ++y) {
                    float pxval = (gains[x, y] - minGain) / maxGain;
                    output.SetPixel(x, size - y - 1, DrawPixel(pxval));
                }
            }
            image.Source = ToImageSource(output);
        }
    }
}