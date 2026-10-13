using System.IO;

using Cavern.Channels;
using Cavern.Waveforms;

namespace Cavern.Format.Measurement {
    /// <summary>
    /// Conversion between recorded measurements and LAF files.
    /// </summary>
    public static class LAFMeasurement {
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
