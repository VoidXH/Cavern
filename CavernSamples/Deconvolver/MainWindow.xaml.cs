using Cavern.Format;
using Cavern.Utilities;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace Deconvolver {
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
            padding.IsChecked = Settings.Default.Padding;
        }

        RIFFWaveReader Import(string fileName) {
            browser.FileName = fileName;
            if (browser.ShowDialog().Value) {
                BinaryReader reader = new BinaryReader(File.OpenRead(browser.FileName));
                return new RIFFWaveReader(reader);
            }
            return null;
        }

        static void Error(string error) => MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        private void LoadFiles(object sender, RoutedEventArgs e) {
            RIFFWaveReader responseReader = Import("Response.wav");
            if (responseReader == null)
                return;
            RIFFWaveReader impulseReader = Import("Impulse.wav");
            if (impulseReader == null)
                return;

            float[] response = responseReader.Read(),
                impulse = impulseReader.Read();
            if (responseReader.SampleRate != impulseReader.SampleRate) {
                Error("The sample rate of the two clips don't match.");
                return;
            }
            int responseChannels = responseReader.ChannelCount,
                impulseChannels = impulseReader.ChannelCount;
            if (impulseChannels != 1 && impulseChannels != responseChannels) {
                Error("The channel count of the two clips don't match. A single-channel impulse is also acceptable.");
                return;
            }

            int fftSize = Math.Max(
                QMath.Base2Ceil((int)responseReader.Length),
                QMath.Base2Ceil((int)impulseReader.Length)
            );

            if (padding.IsChecked.Value) {
                Array.Resize(ref response, fftSize + response.Length);
                Array.Copy(response, 0, response, fftSize, response.Length - fftSize);
                Array.Clear(response, 0, fftSize);

                fftSize = Math.Max(fftSize, QMath.Base2Ceil(response.Length));
            }

            Complex[] impulseFFT = new Complex[fftSize],
                responseFFT = new Complex[fftSize];
            FFTCache cache = new FFTCache(fftSize);
            float[] responseChannel = new float[response.Length / responseChannels];
            for (int channel = 0; channel < responseChannels; ++channel) {
                if (channel < impulseChannels) { // After the channel count check this runs once or for each channel
                    float[] impulseChannel = impulse;
                    if (impulseChannels != 1) {
                        impulseChannel = new float[impulseReader.Length];
                        WaveformUtils.ExtractChannel(impulse, impulseChannel, channel, impulseChannels);
                        Array.Clear(impulseFFT, 0, fftSize);
                    }
                    for (int sample = 0; sample < impulseChannel.Length; ++sample)
                        impulseFFT[sample].Real = impulseChannel[sample];
                    Measurements.InPlaceFFT(impulseFFT, cache);
                }

                if (responseChannels == 1)
                    responseChannel = response;
                else
                    WaveformUtils.ExtractChannel(response, responseChannel, channel, responseChannels);
                if (channel != 1)
                    Array.Clear(responseFFT, 0, fftSize);
                for (int sample = 0; sample < responseChannel.Length; ++sample)
                    responseFFT[sample].Real = responseChannel[sample];
                Measurements.InPlaceFFT(responseFFT, cache);

                for (int sample = 0; sample < fftSize; ++sample)
                    responseFFT[sample].Divide(impulseFFT[sample]);
                Measurements.InPlaceIFFT(responseFFT, cache);
                for (int i = 0, channels = responseChannels; i < responseChannel.Length; ++i)
                    response[channels * i + channel] = responseFFT[i].Real;
            }

            exporter.FileName = "Deconvolved.wav";
            if (exporter.ShowDialog().Value) {
                BinaryWriter handler = new BinaryWriter(File.OpenWrite(exporter.FileName));
                using RIFFWaveWriter writer = new RIFFWaveWriter(handler, responseChannels, responseReader.Length,
                    responseReader.SampleRate, responseReader.Bits);
                writer.Write(response);
            }
        }

        protected override void OnClosed(EventArgs e) {
            Settings.Default.Padding = padding.IsChecked.Value;
            Settings.Default.Save();
            base.OnClosed(e);
        }
    }
}