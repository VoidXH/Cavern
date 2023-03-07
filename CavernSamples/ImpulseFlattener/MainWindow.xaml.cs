using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format;
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

        public MainWindow() {
            InitializeComponent();
            forceFloat.IsChecked = Settings.Default.ForceFloat;
            keepGain.IsChecked = Settings.Default.KeepGain;
            separateExport.IsChecked = Settings.Default.SeparateExport;
            phasePerfect.IsChecked = Settings.Default.PhasePerfect;
            commonEQ.IsChecked = Settings.Default.CommonEQ;
        }

        Convolver GetFilter(Complex[] spectrum, float gain, int sampleRate) {
            Equalizer eq = new Equalizer();
            float[] filterSamples = phasePerfect.IsChecked.Value
                ? eq.GetLinearConvolution(sampleRate, spectrum.Length / 2, gain, spectrum)
                : eq.GetConvolution(sampleRate, spectrum.Length / 2, gain, spectrum);
            return new Convolver(filterSamples, 0);
        }

        void ProcessPerChannel(AudioReader reader, ref float[] impulse) {
            int targetLen = QMath.Base2Ceil((int)reader.Length) << 1;
            Convolver[] filters = new Convolver[reader.ChannelCount];
            FFTCache cache = new FFTCache(targetLen);

            for (int ch = 0; ch < reader.ChannelCount; ++ch) {
                float[] channel = new float[targetLen];
                WaveformUtils.ExtractChannel(impulse, channel, ch, reader.ChannelCount);

                Complex[] spectrum = Measurements.FFT(channel, cache);
                for (int band = 0; band < spectrum.Length; ++band) {
                    spectrum[band] = spectrum[band].Invert();
                }
                filters[ch] = GetFilter(spectrum, WaveformUtils.GetRMS(channel), reader.SampleRate);
            }

            Array.Resize(ref impulse, impulse.Length << 1);
            for (int ch = 0; ch < reader.ChannelCount; ++ch) {
                filters[ch].Process(impulse, ch, reader.ChannelCount);
            }
        }

        void ProcessCommon(AudioReader reader, ref float[] impulse) {
            int targetLen = QMath.Base2Ceil((int)reader.Length) << 1;
            Complex[] commonSpectrum = new Complex[targetLen];
            FFTCache cache = new FFTCache(targetLen);

            for (int ch = 0; ch < reader.ChannelCount; ++ch) {
                float[] channel = new float[targetLen];
                WaveformUtils.ExtractChannel(impulse, channel, ch, reader.ChannelCount);

                Complex[] spectrum = Measurements.FFT(channel, cache);
                for (int band = 0; band < spectrum.Length; ++band) {
                    commonSpectrum[band] += spectrum[band];
                }
            }

            float mul = 1f / reader.ChannelCount;
            for (int band = 0; band < commonSpectrum.Length; ++band) {
                commonSpectrum[band] = (commonSpectrum[band] * mul).Invert();
            }

            Array.Resize(ref impulse, impulse.Length << 1);
            for (int ch = 0; ch < reader.ChannelCount; ++ch) {
                Convolver filter = GetFilter(commonSpectrum, 1, reader.SampleRate);
                filter.Process(impulse, ch, reader.ChannelCount);
            }
        }

        void ProcessImpulse(object sender, RoutedEventArgs e) {
            if (browser.ShowDialog().Value) {
                AudioReader reader = AudioReader.Open(browser.FileName);
                float[] impulse = reader.Read();
                float gain = 1;
                if (keepGain.IsChecked.Value) {
                    gain = WaveformUtils.GetPeak(impulse);
                }

                if (commonEQ.IsChecked.Value) {
                    ProcessCommon(reader, ref impulse);
                } else {
                    ProcessPerChannel(reader, ref impulse);
                }

                if (keepGain.IsChecked.Value) {
                    WaveformUtils.Gain(impulse, gain / WaveformUtils.GetPeak(impulse));
                }

                BitDepth bits = reader.Bits;
                if (forceFloat.IsChecked.Value) {
                    bits = BitDepth.Float32;
                }

                int targetLen = QMath.Base2Ceil((int)reader.Length);
                if (separateExport.IsChecked.Value) {
                    ReferenceChannel[] channels = ChannelPrototype.GetStandardMatrix(reader.ChannelCount);
                    for (int ch = 0; ch < reader.ChannelCount; ++ch) {
                        string exportName = Path.GetFileName(browser.FileName);
                        int idx = exportName.LastIndexOf('.');
                        string channelName = ChannelPrototype.Mapping[(int)channels[ch]].Name;
                        exporter.FileName = $"{exportName[..idx]} - {channelName}{exportName[idx..]}";

                        if (exporter.ShowDialog().Value) {
                            float[] channel = new float[targetLen * 2];
                            WaveformUtils.ExtractChannel(impulse, channel, ch, reader.ChannelCount);
                            Stream outStream = File.OpenWrite(exporter.FileName);
                            new RIFFWaveWriter(outStream, 1, targetLen * 2, reader.SampleRate, bits).Write(channel);
                        }
                    }
                } else {
                    exporter.FileName = Path.GetFileName(browser.FileName);
                    if (exporter.ShowDialog().Value) {
                        Stream outStream = File.OpenWrite(exporter.FileName);
                        new RIFFWaveWriter(outStream, reader.ChannelCount, targetLen * 2, reader.SampleRate, bits).Write(impulse);
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e) {
            Settings.Default.ForceFloat = forceFloat.IsChecked.Value;
            Settings.Default.KeepGain = keepGain.IsChecked.Value;
            Settings.Default.SeparateExport = separateExport.IsChecked.Value;
            Settings.Default.PhasePerfect = phasePerfect.IsChecked.Value;
            Settings.Default.CommonEQ = commonEQ.IsChecked.Value;
            Settings.Default.Save();
            base.OnClosed(e);
        }
    }
}