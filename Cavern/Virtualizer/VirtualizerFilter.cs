using System;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Virtualizer {
    /// <summary>
    /// Convolution filters for each ear and virtual channel to simulate a spatial environment.
    /// </summary>
    public partial class VirtualizerFilter {
        /// <summary>
        /// Sample rate of the recorded HRIR filters. Only this system sample rate is allowed for virtualization.
        /// </summary>
        public static int FilterSampleRate { get; private set; } = defaultSampleRate;

        /// <summary>
        /// The active filter set's impulses were measured at this distance, meters.
        /// </summary>
        public static float ReferenceDistance { get; private set; } = defaultDistance;

        /// <summary>
        /// Number of virtual channels.
        /// </summary>
        public static int VirtualChannels => SpatialChannels.Length;

        /// <summary>
        /// Active virtualized channels and their corresponding filters.
        /// </summary>
        static VirtualChannel[] SpatialChannels {
            get => spatialChannels ??= GetDefaultHRIRSet();
            set => spatialChannels = value;
        }
        static VirtualChannel[] spatialChannels;

        /// <summary>
        /// Cache of each output channel.
        /// </summary>
        float[][] originalSplit;

        /// <summary>
        /// Cache of each output channel for one ear.
        /// </summary>
        float[][] leftSplit, rightSplit;

        /// <summary>
        /// Length of split arrays.
        /// </summary>
        int blockSize;

        /// <summary>
        /// Cached channel IDs for center hack.
        /// </summary>
        int left = unassigned, right = unassigned, center = unassigned;

        /// <summary>
        /// Center hack: add a 7.5 ms -20 dB delay of the center to fronts to simulate a wall echo for better immersion.
        /// </summary>
        Delay centerDelay;

        /// <summary>
        /// Delayed center channel signal for center hack.
        /// </summary>
        float[] delayedCenter;

        /// <summary>
        /// Parallel executor of channel filtering.
        /// </summary>
        readonly Parallelizer processor;

        /// <summary>
        /// Convolution filters for each ear and virtual channel to simulate a spatial environment.
        /// </summary>
        public VirtualizerFilter() => processor = new Parallelizer(ProcessChannel);

        /// <summary>
        /// Apply a new set of HRIR filters. The reference distance of the sound sources from the subject will be 1 meter.
        /// </summary>
        public static void Override(VirtualChannel[] channels, int sampleRate) => Override(channels, sampleRate, 1);

        /// <summary>
        /// Apply a new set of HRIR filters, and specify what distance from the subject was the speaker when they were created.
        /// </summary>
        public static void Override(VirtualChannel[] channels, int sampleRate, float referenceDistance) {
            SpatialChannels = channels;
            FilterSampleRate = sampleRate;
            ReferenceDistance = referenceDistance;
        }

        /// <summary>
        /// Restore the default HRIR filter set.
        /// </summary>
        public static void Reset() {
            SpatialChannels = GetDefaultHRIRSet();
            FilterSampleRate = defaultSampleRate;
            ReferenceDistance = defaultDistance;
        }

        /// <summary>
        /// Set up virtual channel set for the virtualization filters.
        /// </summary>
        public void SetLayout() {
            bool setAgain = centerDelay == null;
            if (Listener.Channels.Length == SpatialChannels.Length) {
                for (int channel = 0; channel < SpatialChannels.Length; ++channel) {
                    if (Listener.Channels[channel].X != SpatialChannels[channel].x ||
                        Listener.Channels[channel].Y != SpatialChannels[channel].y) {
                        setAgain = true;
                        break;
                    }
                }
            } else {
                setAgain = true;
            }
            if (!setAgain) {
                return;
            }

            centerDelay = new Delay(.0075f, FilterSampleRate);
            Channel[] newChannels = new Channel[SpatialChannels.Length];
            for (int channel = 0; channel < SpatialChannels.Length; ++channel) {
                newChannels[channel] = new Channel(SpatialChannels[channel].x, SpatialChannels[channel].y);
                if (SpatialChannels[channel].x == 0) {
                    if (SpatialChannels[channel].y == 0) {
                        center = channel;
                    } else if (SpatialChannels[channel].y == -45) {
                        left = channel;
                    } else if (SpatialChannels[channel].y == 45) {
                        right = channel;
                    }
                }
            }
            Listener.ReplaceChannels(newChannels);
            originalSplit = new float[SpatialChannels.Length][];
            leftSplit = new float[SpatialChannels.Length][];
            rightSplit = new float[SpatialChannels.Length][];
            for (int channel = 0; channel < SpatialChannels.Length; ++channel) {
                rightSplit[channel] = new float[0];
            }
            originalSplit[0] = new float[0];
        }

        /// <summary>
        /// Apply the virtualizer on the <see cref="Listener"/>'s output,
        /// if the configuration matches the virtualization layout and filter sample rate.
        /// </summary>
        public void Process(float[] output, int sampleRate) {
            int channels = Listener.Channels.Length;
            blockSize = output.Length / channels;
            if (originalSplit == null || sampleRate != FilterSampleRate) {
                return;
            }

            if (originalSplit[0].Length != blockSize) {
                for (int channel = 0; channel < channels; ++channel) {
                    originalSplit[channel] = new float[blockSize];
                }
                delayedCenter = new float[blockSize];
            }

            for (int sample = 0, outSample = 0; sample < blockSize; ++sample) {
                for (int channel = 0; channel < channels; ++channel, ++outSample) {
                    originalSplit[channel][sample] = output[outSample];
                }
            }

            if (center != unassigned) {
                Array.Copy(originalSplit[center], delayedCenter, blockSize);
                centerDelay.Process(delayedCenter); // Add 7.5 ms delay
                WaveformUtils.Gain(delayedCenter, .1f); // -20 dB gain
                if (left != unassigned && right != unassigned) { // Simulate front wall to convey actual forward direction
                    WaveformUtils.Mix(delayedCenter, originalSplit[left]);
                    WaveformUtils.Mix(delayedCenter, originalSplit[right]);
                }
            }
            processor.For(0, channels);

            // Stereo downmix
            output.Clear();
            for (int sample = 0; sample < blockSize; ++sample) {
                int leftOut = sample * channels, rightOut = leftOut + 1;
                for (int channel = 0; channel < channels; ++channel) {
                    float unspatialized = originalSplit[channel][sample] * .5f;
                    output[leftOut] += leftSplit[channel][sample] + unspatialized;
                    output[rightOut] += rightSplit[channel][sample] + unspatialized;
                }
            }
        }

        /// <summary>
        /// Split and convolve a single channel by ID.
        /// </summary>
        void ProcessChannel(int channel) {
            // Select the retain range
            Crossover lowCrossover = SpatialChannels[channel].Crossover;
            lowCrossover.Process(originalSplit[channel]);
            originalSplit[channel] = lowCrossover.LowOutput;

            // Select the impulse response frequency range
            if (rightSplit[channel].Length != blockSize) {
                rightSplit[channel] = new float[blockSize];
            }
            leftSplit[channel] = lowCrossover.HighOutput;
            Array.Copy(leftSplit[channel], rightSplit[channel], blockSize);
            SpatialChannels[channel].Filter.Process(leftSplit[channel], rightSplit[channel]);
        }

        /// <summary>
        /// Marker instead of a channel ID when a channel was not set.
        /// </summary>
        const int unassigned = -1;
    }
}