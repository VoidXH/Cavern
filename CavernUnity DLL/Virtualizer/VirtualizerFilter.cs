using System;
using System.Threading.Tasks;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Virtualizer {
    /// <summary>Convolution filters for each ear and virtual channel to simulate a spatial environment.</summary>
    static partial class VirtualizerFilter {
        const int filterSampleRate = 48000, unassigned = -1;
        /// <summary>Cache of each output channel.</summary>
        static float[][] originalSplit;
        /// <summary>Cache of each output channel for one ear.</summary>
        static float[][] leftSplit, rightSplit;
        /// <summary>Length of split arrays.</summary>
        static int blockSize;
        /// <summary>Cached channel IDs for center hack.</summary>
        static int left = unassigned, right = unassigned, center = unassigned;
        /// <summary>Center hack: add a 7.5 ms -20 dB delay of the center to fronts to simulate a wall echo for better immersion.</summary>
        static Delay centerDelay;
        /// <summary>Delayed center channel signal for center hack.</summary>
        static float[] delayedCenter;

        /// <summary>Set up virtual channel set for the virtualization filters.</summary>
        public static void SetLayout() {
            bool setAgain = centerDelay == null;
            if (Listener.Channels.Length == spatialChannels.Length) {
                for (int channel = 0; channel < spatialChannels.Length; ++channel) {
                    if (Listener.Channels[channel].X != spatialChannels[channel].X || Listener.Channels[channel].Y != spatialChannels[channel].Y) {
                        setAgain = true;
                        break;
                    }
                }
            } else
                setAgain = true;
            if (!setAgain)
                return;
            centerDelay = new Delay(.0075f, filterSampleRate);
            Channel[] newChannels = new Channel[spatialChannels.Length];
            for (int channel = 0; channel < spatialChannels.Length; ++channel) {
                newChannels[channel] = new Channel(spatialChannels[channel].X, spatialChannels[channel].Y);
                if (spatialChannels[channel].X == 0) {
                    if (spatialChannels[channel].Y == 0)
                        center = channel;
                    else if (spatialChannels[channel].Y == -45)
                        left = channel;
                    else if (spatialChannels[channel].Y == 45)
                        right = channel;
                }
            }
            Listener.Channels = newChannels;
            originalSplit = new float[spatialChannels.Length][];
            leftSplit = new float[spatialChannels.Length][];
            rightSplit = new float[spatialChannels.Length][];
            for (int channel = 0; channel < spatialChannels.Length; ++channel)
                rightSplit[channel] = new float[0];
            originalSplit[0] = new float[0];
        }

        /// <summary>Split and convolve a single channel by ID.</summary>
        static void ProcessChannel(int channel) {
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
            Buffer.BlockCopy(leftSplit[channel], 0, rightSplit[channel], 0, blockSize * sizeof(float));
            spatialChannels[channel].LeftFilter.Process(leftSplit[channel]);
            spatialChannels[channel].RightFilter.Process(rightSplit[channel]);
        }

        /// <summary>Apply the virtualizer on the <see cref="AudioListener3D"/>'s output,
        /// if the configuration matches the virtualization layout and filter sample rate.</summary>
        public static void Process(float[] output) {
            int channels = Listener.Channels.Length;
            blockSize = output.Length / channels;
            if (originalSplit == null || AudioListener3D.Current.SampleRate != filterSampleRate)
                return;
            if (originalSplit[0].Length != blockSize) {
                for (int channel = 0; channel < channels; ++channel)
                    originalSplit[channel] = new float[blockSize];
                delayedCenter = new float[blockSize];
            }
            for (int sample = 0, outSample = 0; sample < blockSize; ++sample)
                for (int channel = 0; channel < channels; ++channel, ++outSample)
                    originalSplit[channel][sample] = output[outSample];
            if (center != unassigned) { // TODO: do this for all front/rear center channels
                Buffer.BlockCopy(originalSplit[center], 0, delayedCenter, 0, blockSize * sizeof(float));
                centerDelay.Process(delayedCenter); // Add 7.5 ms delay
                WaveformUtils.Gain(delayedCenter, .1f); // -20 dB gain
                if (left != unassigned && right != unassigned) { // Simulate front wall to convey actual forward direction
                    WaveformUtils.Mix(delayedCenter, originalSplit[left]);
                    WaveformUtils.Mix(delayedCenter, originalSplit[right]);
                }
            }
            Parallel.For(0, channels, ProcessChannel);
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