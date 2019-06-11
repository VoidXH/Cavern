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
            Channel[] newChannels = new Channel[spatialChannels.Length];
            for (int channel = 0; channel < spatialChannels.Length; ++channel)
                newChannels[channel] = new Channel(spatialChannels[channel].X, spatialChannels[channel].Y);
            Listener.Channels = newChannels;
            if (originalSplit == null) {
                originalSplit = new float[spatialChannels.Length][];
                leftSplit = new float[spatialChannels.Length][];
                rightSplit = new float[spatialChannels.Length][];
                for (int channel = 0; channel < spatialChannels.Length; ++channel)
                    rightSplit[channel] = new float[0];
                originalSplit[0] = new float[0];
            }
        }

        /// <summary>Apply the virtualizer on the <see cref="AudioListener3D"/>'s output,
        /// if the configuration matches the virtualization layout and filter sample rate.</summary>
        public static void Process(float[] output) {
            int channels = Listener.Channels.Length, blockSize = output.Length / channels, blockCopySize = blockSize * sizeof(float);
            if (originalSplit == null || AudioListener3D.Current.SampleRate != filterSampleRate)
                return;
            if (originalSplit[0].Length != blockSize)
                for (int channel = 0; channel < channels; ++channel)
                    originalSplit[channel] = new float[blockSize];
            for (int sample = 0, outSample = 0; sample < blockSize; ++sample)
                for (int channel = 0; channel < channels; ++channel, ++outSample)
                    originalSplit[channel][sample] = output[outSample];
            // Convolution
            Parallel.For(0, channels, channel => {
                // Select the retain range
                Crossover lowCrossover = spatialChannels[channel].LowCrossover, highCrossover = spatialChannels[channel].HighCrossover;
                lowCrossover.Process(originalSplit[channel]);
                spatialChannels[channel].HighCrossover.Process(lowCrossover.HighOutput);
                originalSplit[channel] = lowCrossover.LowOutput;
                for (int sample = 0; sample < blockSize; ++sample)
                    originalSplit[channel][sample] += highCrossover.HighOutput[sample];
                // Select the impulse response frequency range
                if (rightSplit[channel].Length != blockSize)
                    rightSplit[channel] = new float[blockSize];
                leftSplit[channel] = highCrossover.LowOutput;
                Buffer.BlockCopy(leftSplit[channel], 0, rightSplit[channel], 0, blockCopySize);
                spatialChannels[channel].LeftFilter.Process(leftSplit[channel]);
                spatialChannels[channel].RightFilter.Process(rightSplit[channel]);
            });
            // Stereo downmix
            Array.Clear(output, 0, output.Length);
            for (int sample = 0; sample < blockSize; ++sample) {
                int leftOut = sample * channels, rightOut = leftOut + 1;
                for (int channel = 0; channel < channels; ++channel) {
                    float unspatialized = originalSplit[channel][sample] * .5f;
                    output[leftOut] += leftSplit[channel][sample] + unspatialized;
                    output[rightOut] += rightSplit[channel][sample] + unspatialized;
                }
            }
        }
    }
}