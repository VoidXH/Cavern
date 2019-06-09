using System;
using System.Threading.Tasks;

using Cavern.Filters;

namespace Cavern.Virtualizer {
    /// <summary>Convolution filters for each ear and virtual channel to simulate a spatial environment.</summary>
    static partial class VirtualizerFilter {
        const int filterSampleRate = 48000;
        /// <summary>Cache of each output channel.</summary>
        static float[][] originalSplit;
        /// <summary>Cache of each output channel for one ear.</summary>
        static float[][] leftSplit, rightSplit;

        /// <summary>Set up virtual channel set for the virtualization filters.</summary>
        public static void SetLayout() {
            int channels = spatialChannels.Length, updateRate = AudioListener3D.Current.UpdateRate;
            Channel[] newChannels = new Channel[channels];
            for (int channel = 0; channel < channels; ++channel)
                newChannels[channel] = new Channel(spatialChannels[channel].X, spatialChannels[channel].Y);
            AudioListener3D.Channels = newChannels;
            if (originalSplit == null) {
                originalSplit = new float[channels][];
                leftSplit = new float[channels][];
                rightSplit = new float[channels][];
            }
            if (originalSplit[0] == null || originalSplit[0].Length != updateRate) {
                for (int channel = 0; channel < channels; ++channel) {
                    originalSplit[channel] = new float[updateRate];
                    leftSplit[channel] = new float[updateRate];
                    rightSplit[channel] = new float[updateRate];
                }
            }
        }

        /// <summary>Apply the virtualizer on the <see cref="AudioListener3D"/>'s output,
        /// if the configuration matches the virtualization layout and filter sample rate.</summary>
        public static void Process(float[] output) {
            int channelCount = AudioListener3D.Channels.Length, updateRate = AudioListener3D.Current.UpdateRate, blockCopySize = updateRate * sizeof(float);
            if (AudioListener3D.Current.SampleRate != filterSampleRate)
                return;
            int outputSample = 0;
            for (int sample = 0; sample < updateRate; ++sample)
                for (int channel = 0; channel < channelCount; ++channel)
                    originalSplit[channel][sample] = output[outputSample++];
            // Convolution
            Parallel.For(0, channelCount, channel => {
                // Select the retain range
                Crossover lowCrossover = spatialChannels[channel].LowCrossover, highCrossover = spatialChannels[channel].HighCrossover;
                lowCrossover.Process(originalSplit[channel]);
                spatialChannels[channel].HighCrossover.Process(lowCrossover.HighOutput);
                originalSplit[channel] = lowCrossover.LowOutput;
                for (int sample = 0; sample < updateRate; ++sample)
                    originalSplit[channel][sample] += highCrossover.HighOutput[sample];
                // Select the impulse response frequency range
                leftSplit[channel] = highCrossover.LowOutput;
                Buffer.BlockCopy(leftSplit[channel], 0, rightSplit[channel], 0, blockCopySize);
                spatialChannels[channel].LeftFilter.Process(leftSplit[channel]);
                spatialChannels[channel].RightFilter.Process(rightSplit[channel]);
            });
            // Stereo downmix
            Array.Clear(output, 0, output.Length);
            for (int sample = 0; sample < updateRate; ++sample) {
                int leftOut = sample * channelCount, rightOut = leftOut + 1;
                for (int channel = 0; channel < channelCount; ++channel) {
                    float unspatialized = originalSplit[channel][sample] * .5f;
                    output[leftOut] += leftSplit[channel][sample] + unspatialized;
                    output[rightOut] += rightSplit[channel][sample] + unspatialized;
                }
            }
        }
    }
}