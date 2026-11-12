using System;
using System.IO;

using Cavern.Channels;
using Cavern.QuickEQ.Measurement;
using Cavern.QuickEQ.SignalGeneration;
using Cavern.Utilities;
using Cavern.Utilities.Enums;
using Cavern.Waveforms;

namespace Cavern.Format.Measurements {
    /// <summary>
    /// Represents a Cavern measurement file.
    /// </summary>
    public class LAFMeasurement : Measurement {
        /// <summary>
        /// Which channels are present in this format and with what name.
        /// </summary>
        public override (Channel channel, string name)[] Channels { get; protected set; }

        /// <summary>
        /// Represents a Cavern measurement file.
        /// </summary>
        public LAFMeasurement() { }

        /// <summary>
        /// Import a Cavern measurement file using the default QuickEQ sweep (20 Hz to 20 kHz) and no resampling.
        /// </summary>
        /// <param name="path">Path to the LAF measurement file</param>
        public LAFMeasurement(string path) : this(path, 20, 20000, -1) { }

        /// <summary>
        /// Import a Cavern measurement file with no resampling.
        /// </summary>
        /// <param name="path">Path to the LAF measurement file</param>
        /// <param name="sweepStartFreq">Start frequency in Hz of the sweep the excitements were recorded with</param>
        /// <param name="sweepEndFreq">End frequency in Hz of the sweep the excitements were recorded with</param>
        public LAFMeasurement(string path, double sweepStartFreq, double sweepEndFreq) : this(path, sweepStartFreq, sweepEndFreq, -1) { }

        /// <summary>
        /// Import a Cavern measurement file.
        /// </summary>
        /// <param name="path">Path to the LAF measurement file</param>
        /// <param name="sweepStartFreq">Start frequency in Hz of the sweep the excitements were recorded with</param>
        /// <param name="sweepEndFreq">End frequency in Hz of the sweep the excitements were recorded with</param>
        /// <param name="resample">Import the measurement at this sample rate, -1 skips it</param>
        public LAFMeasurement(string path, double sweepStartFreq, double sweepEndFreq, int resample) {
            using AudioReader reader = AudioReader.Open(path);
            reader.ReadHeader();
            SampleRate = reader.SampleRate;
            MultichannelWaveform data = new MultichannelWaveform(reader.ReadMultichannelAfterHeader());

            if (resample != -1) {
                data = data.Resample(SampleRate, resample, ResampleAlgorithm.CatmullRom);
                SampleRate = resample;
            }

            int channels = GetChannelCount(data);
            int measurements = data.Channels / channels;
            int samplesPerCh = data.Length / channels;
            float[] sweep = SweepGenerator.Frame(SweepGenerator.Exponential(sweepStartFreq, sweepEndFreq, samplesPerCh / 2, SampleRate));
            using FFTCache cache = new FFTCache(samplesPerCh);
            ReferenceChannel[] references = reader.GetRenderer().GetChannels();
            Channels = new (Channel, string)[channels * measurements];
            Positions = new MeasuredPosition[measurements];
            for (int measurement = 0; measurement < measurements; measurement++) {
                float[][] impulseResponses = new float[channels][];
                for (int channel = 0; channel < channels; channel++) {
                    float[] recording = new float[samplesPerCh];
                    Array.Copy(data[channel + measurement * channels], samplesPerCh * channel, recording, 0, samplesPerCh);
                    impulseResponses[channel] = Cavern.Utilities.Measurements.GetRealPart(Cavern.Utilities.Measurements.GetImpulseResponse(sweep, recording, cache));
                    ChannelPrototype prototype = ChannelPrototype.Mapping[(int)references[channel]];
                    Channels[measurement * channels + channel] = (new Channel(prototype), ChannelPrototype.GetNames(new[] { references[channel] })[0]);
                }
                Positions[measurement] = new MeasuredPosition(measurement, new MultichannelWaveform(impulseResponses), SampleRate, true);
            }
        }

        /// <summary>
        /// Determine the number of channels per measurement from the raw data of a Cavern measurement file.
        /// </summary>
        /// <param name="data">Multichannel data read from the file</param>
        /// <returns>Number of channels per measurement</returns>
        public static int GetChannelCount(MultichannelWaveform data) {
            int channels = 0;
            int lastSample = data.Length - 1;
            while (data[0][lastSample] == 0 && lastSample > 0) {
                lastSample--;
            }
            for (int channel = 1; channel < data.Channels; channel++) {
                int firstSample = 0;
                while (data[channel][firstSample] == 0 && firstSample < lastSample) {
                    firstSample++;
                }
                if (firstSample < lastSample) {
                    channels = channel;
                    break;
                }
            }
            if (channels == 0) {
                channels = data.Channels;
            }
            return channels;
        }

        /// <summary>
        /// Pack a measurement set into a LAF file and export it to a stream.
        /// </summary>
        /// <param name="stream">Stream to export the LAF file to</param>
        /// <param name="excitements">The recorded measurement of each channel as [measurement no.][channel][sample]</param>
        /// <param name="sampleRate">Sample rate of the measurements</param>
        public static void Export(Stream stream, MultichannelWaveform[] excitements, int sampleRate) {
            Channel[] channels = new Channel[excitements.Length * excitements[0].Channels];
            for (int i = 0; i < excitements[0].Channels; i++) {
                Channel channel;
                if (excitements[0].Channels == Listener.Channels.Length) {
                    channel = Listener.Channels[i];
                } else {
                    ChannelPrototype prototype = ChannelPrototype.Get(i, excitements[0].Channels);
                    channel = new Channel(prototype.X, prototype.Y, prototype.LFE);
                }

                for (int j = 0; j < excitements.Length; j++) {
                    channels[i + excitements[0].Channels * j] = channel;
                }
            }

            int current = 0;
            float[][] channelSamples = new float[channels.Length][];
            for (int excitement = 0; excitement < excitements.Length; excitement++) {
                MultichannelWaveform excitementChannels = excitements[excitement];
                for (int channel = 0; channel < excitementChannels.Channels; channel++) {
                    channelSamples[current++] = excitementChannels[channel];
                }
            }
            LimitlessAudioFormatWriter writer = new LimitlessAudioFormatWriter(stream, channelSamples[0].LongLength, sampleRate, BitDepth.Float32, channels);
            writer.WriteOffset(channelSamples, excitements[0].Channels);
        }

        /// <summary>
        /// Pack a measurement set into a LAF file and export it to the file system.
        /// </summary>
        /// <param name="path">Path to the exported LAF file</param>
        /// <param name="excitements">The recorded measurement of each channel as [measurement no.][channel][sample]</param>
        /// <param name="sampleRate">Sample rate of the measurements</param>
        public static void Export(string path, MultichannelWaveform[] excitements, int sampleRate) {
            using FileStream stream = File.Open(path, FileMode.Create);
            Export(stream, excitements, sampleRate);
        }
    }
}
