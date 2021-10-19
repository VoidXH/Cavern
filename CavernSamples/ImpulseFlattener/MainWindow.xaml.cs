using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

using Cavern.Filters;
using Cavern.Format;
using Cavern.QuickEQ.Equalization;
using Cavern.Remapping;
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
                int targetLen = 1 << QMath.Log2((int)reader.Length);
                if (targetLen != reader.Length)
                    targetLen <<= 1;
                Convolver[] filters = new Convolver[reader.ChannelCount];

                for (int ch = 0; ch < reader.ChannelCount; ++ch) {
                    float[] channel = new float[targetLen];
                    WaveformUtils.ExtractChannel(impulse, channel, ch, reader.ChannelCount);

                    float[] spectrum = Measurements.GetSpectrum(Measurements.FFT(channel));
                    double step = reader.SampleRate * .5 / spectrum.Length;
                    Equalizer eq = new Equalizer();
                    for (int i = 0; i < spectrum.Length; ++i)
                        eq.AddBand(new Band(i * step, -20 * Math.Log10(spectrum[i])));

                    Array.Resize(ref channel, targetLen << 1);
                    float gain = 1;
                    if (normalizeToPeak.IsChecked.Value) {
                        gain = 0;
                        for (int i = 0; i < reader.Length; ++i) {
                            float abs = Math.Abs(channel[i]);
                            if (gain < abs)
                                gain = abs;
                        }
                    }
                    float[] filterSamples = phasePerfect.IsChecked.Value
                        ? eq.GetLinearConvolution(reader.SampleRate, targetLen, gain)
                        : eq.GetConvolution(reader.SampleRate, targetLen, gain);
                    filters[ch] = new Convolver(filterSamples, 0);
                }

                Array.Resize(ref impulse, targetLen * reader.ChannelCount * 2);
                for (int ch = 0; ch < reader.ChannelCount; ++ch)
                    filters[ch].Process(impulse, ch, reader.ChannelCount);

                BitDepth bits = reader.Bits;
                if (forceFloat.IsChecked.Value)
                    bits = BitDepth.Float32;
                if (separateExport.IsChecked.Value) {
                    ReferenceChannel[] channels = ChannelPrototype.StandardMatrix[reader.ChannelCount];
                    for (int ch = 0; ch < reader.ChannelCount; ++ch) {
                        string exportName = Path.GetFileName(browser.FileName);
                        int idx = exportName.LastIndexOf('.');
                        string channelName = ChannelPrototype.Mapping[(int)channels[ch]].Name;
                        exporter.FileName = $"{exportName.Substring(0, idx)} - {channelName}{exportName.Substring(idx)}";

                        if (exporter.ShowDialog().Value) {
                            float[] channel = new float[targetLen * 2];
                            WaveformUtils.ExtractChannel(impulse, channel, ch, reader.ChannelCount);
                            BinaryWriter outStream = new BinaryWriter(File.Open(exporter.FileName, FileMode.Create));
                            new RIFFWaveWriter(outStream, 1, targetLen, reader.SampleRate, bits).Write(channel);
                        }
                    }
                } else {
                    exporter.FileName = Path.GetFileName(browser.FileName);
                    if (exporter.ShowDialog().Value) {
                        BinaryWriter outStream = new BinaryWriter(File.Open(exporter.FileName, FileMode.Create));
                        new RIFFWaveWriter(outStream, reader.ChannelCount, targetLen, reader.SampleRate, bits).Write(impulse);
                    }
                }
            }
        }
    }
}