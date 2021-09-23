using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

using Cavern.Filters;
using Cavern.Format;
using Cavern.QuickEQ;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

using Window = System.Windows.Window;

namespace ImpulseFlattener {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        readonly OpenFileDialog browser = new OpenFileDialog {
            Filter = "RIFF WAVE files (*.wav)|*.wav"
        };

        readonly SaveFileDialog exporter = new SaveFileDialog {
            Filter = "RIFF WAVE files (*.wav)|*.wav"
        };

        public MainWindow() => InitializeComponent();

        void ProcessImpulse(object sender, RoutedEventArgs e) {
            if (browser.ShowDialog().Value) {
                BinaryReader stream = new BinaryReader(File.Open(browser.FileName, FileMode.Open));
                RIFFWaveReader reader = new RIFFWaveReader(stream);
                float[] impulse = reader.Read();
                int logLen = QMath.Log2((int)reader.Length),
                    targetLen = 1 << logLen;
                if (reader.Length != targetLen)
                    targetLen <<= 1;
                Array.Resize(ref impulse, targetLen);

                float[] spectrum = Measurements.GetSpectrum(Measurements.FFT(impulse));
                double step = reader.SampleRate * .5 / spectrum.Length;
                Equalizer eq = new Equalizer();
                for (int i = 0; i < spectrum.Length; ++i)
                    eq.AddBand(new Band(i * step, -20 * Math.Log10(spectrum[i])));

                Array.Resize(ref impulse, impulse.Length << 1);
                float gain = 1;
                if (normalizeToPeak.IsChecked.Value) {
                    gain = 0;
                    for (int i = 0; i < reader.Length; ++i) {
                        float abs = Math.Abs(impulse[i]);
                        if (gain < abs)
                            gain = abs;
                    }
                }
                float[] filterSamples = eq.GetConvolution(reader.SampleRate, impulse.Length, gain);
                Convolver filter = new Convolver(filterSamples, 0);
                filter.Process(impulse);

                exporter.FileName = Path.GetFileName(browser.FileName);
                if (exporter.ShowDialog().Value) {
                    BinaryWriter outStream = new BinaryWriter(File.Open(exporter.FileName, FileMode.Create));
                    BitDepth bits = reader.Bits;
                    if (forceFloat.IsChecked.Value)
                        bits = BitDepth.Float32;
                    new RIFFWaveWriter(outStream, reader.ChannelCount, reader.Length, reader.SampleRate, bits).Write(impulse);
                }
            }
        }
    }
}