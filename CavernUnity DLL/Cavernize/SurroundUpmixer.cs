using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Cavern.Channels;
using Cavern.Utilities;

namespace Cavern.Cavernize {
    /// <summary>
    /// Creates 5.1 or 7.1 mixes from any legacy stream by matrix upmixing. Keeps any additional channels.
    /// </summary>
    public class SurroundUpmixer {
        /// <summary>
        /// Required data for handling a single channel in the upmixing process.
        /// </summary>
        struct ChannelData : IEquatable<ChannelData> {
            public ReferenceChannel target;
            public bool writtenTo;
            public float[] lastSamples;

            public ChannelData(ReferenceChannel target) {
                this.target = target;
                writtenTo = false;
                lastSamples = null;
            }

            /// <summary>
            /// Check if two channel data holders represent the same channel.
            /// </summary>
            public bool Equals(ChannelData other) => target == other.target;
        }

        /// <summary>
        /// Called when a non-<see cref="loop"/>ing <see cref="source"/> has generated its last sample blocks.
        /// </summary>
        public event Action OnPlaybackFinished;

        /// <summary>
        /// Creates missing channels from existing ones. Works best if the source is matrix-encoded.
        /// </summary>
        /// <remarks>Not recommended for Gaming 3D setups, as the rear channels are inverses of each other and this will
        /// cancel out for a single rear channel.</remarks>
        public bool matrixUpmix = true;

        /// <summary>
        /// Restart the source when finished.
        /// </summary>
        public bool loop;

        /// <summary>
        /// Playback position in samples.
        /// </summary>
        public int timeSamples;

        /// <summary>
        /// Stream to be upmixed.
        /// </summary>
        /// <remarks>A legacy channel layout found in <see cref="ChannelPrototype.GetStandardMatrix(int)"/> is required.</remarks>
        readonly Clip source;

        /// <summary>
        /// Required data per channel for handling the upmixing process.
        /// </summary>
        ChannelData[] output;

        /// <summary>
        /// Cached samples from the <see cref="source"/> for processing.
        /// </summary>
        float[][] cache;

        /// <summary>
        /// Creates 5.1 or 7.1 mixes from any legacy stream by matrix upmixing. Keeps any additional channels.
        /// </summary>
        public SurroundUpmixer(Clip source) {
            this.source = source;
            Init();
        }

        /// <summary>
        /// Creates 5.1 or 7.1 mixes from any legacy stream by matrix upmixing. Keeps any additional channels.
        /// </summary>
        public SurroundUpmixer(Clip source, bool matrixUpmix) {
            this.source = source;
            this.matrixUpmix = matrixUpmix;
            Init();
        }

        /// <summary>
        /// Generate the next <paramref name="samples"/> samples to be retrieved with <see cref="RetrieveSamples(ReferenceChannel)"/>.
        /// </summary>
        public void GenerateSamples(int samples) {
            // Preparations
            ReferenceChannel[] layout = ChannelPrototype.GetStandardMatrix(source.Channels);
            if (output[0].lastSamples.Length != samples) {
                for (int channel = 0; channel < output.Length; ++channel) {
                    output[channel].lastSamples = null;
                }

                cache = new float[source.Channels][];
                for (int channel = 0; channel < source.Channels; ++channel) {
                    cache[channel] = new float[samples];
                    int channelId = GetChannelId(layout[channel]);
                    if (channelId != -1) {
                        output[channelId].lastSamples = cache[channel]; // Cache linked with output, no need to copy
                    }
                }

                for (int channel = 0; channel < output.Length; ++channel) {
                    if (output[channel].lastSamples == null) {
                        output[channel].lastSamples = new float[samples];
                    }
                }
            }
            for (int channel = 0; channel < output.Length; ++channel) {
                output[channel].writtenTo = false;
            }

            // Fetch samples
            source.GetData(new MultichannelWaveform(cache), timeSamples);
            if (timeSamples >= source.Samples) {
                if (loop) {
                    timeSamples %= source.Samples;
                } else {
                    timeSamples = 0;
                    OnPlaybackFinished();
                }
            }
            timeSamples += samples;
            for (int channel = 0; channel < source.Channels; ++channel) {
                output[GetChannelId(layout[channel])].writtenTo = true;
            }

            // Create missing channels via matrix if asked for
            if (matrixUpmix) {
                if (output[0].writtenTo && output[1].writtenTo) { // Left and right channels available
                    if (!output[2].writtenTo) { // Create discrete middle channel
                        float[] left = output[0].lastSamples, right = output[1].lastSamples, center = output[2].lastSamples;
                        for (int offset = 0; offset < samples; ++offset) {
                            center[offset] = (left[offset] + right[offset]) * .5f;
                        }
                        output[2].writtenTo = true;
                    }
                    if (!output[6].writtenTo) { // Matrix mix for sides
                        float[] leftFront = output[0].lastSamples, rightFront = output[1].lastSamples,
                            leftSide = output[6].lastSamples, rightSide = output[7].lastSamples;
                        for (int offset = 0; offset < samples; ++offset) {
                            leftSide[offset] = (leftFront[offset] - rightFront[offset]) * .5f;
                            rightSide[offset] = -leftSide[offset];
                        }
                        output[6].writtenTo = output[7].writtenTo = true;
                    }
                    if (!output[4].writtenTo) { // Extend sides to rears...
                        bool rearsAvailable = false; // ...but only if there are rears in the system
                        for (int channel = 0; channel < Listener.Channels.Length; ++channel) {
                            float currentY = Listener.Channels[channel].Y;
                            if (currentY < -135 || currentY > 135) {
                                rearsAvailable = true;
                                break;
                            }
                        }
                        if (rearsAvailable) {
                            float[] leftSide = output[6].lastSamples, rightSide = output[7].lastSamples,
                                leftRear = output[4].lastSamples, rightRear = output[5].lastSamples;
                            for (int offset = 0; offset < samples; ++offset) {
                                leftRear[offset] = leftSide[offset] *= .5f;
                                rightRear[offset] = rightSide[offset] *= .5f;
                            }
                            output[4].writtenTo = output[5].writtenTo = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns an array of all channels that are required to play this upmixed <see cref="source"/>.
        /// </summary>
        public ReferenceChannel[] GetChannels() {
            ReferenceChannel[] result = new ReferenceChannel[output.Length];
            for (int channel = 0; channel < output.Length; ++channel) {
                result[channel] = output[channel].target;
            }
            return result;
        }

        /// <summary>
        /// Returns if the given channel was updated the last time <see cref="GenerateSamples(int)"/> was called.
        /// </summary>
        public bool Readable(ReferenceChannel channel) {
            if ((int)channel < 8) {
                return output[(int)channel].writtenTo;
            }
            for (int i = 8; i < output.Length; ++i) {
                if (output[i].target == channel) {
                    return output[i].writtenTo;
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieve samples generated with <see cref="GenerateSamples(int)"/>.
        /// </summary>
        /// <remarks>This function doesn't check if a channel contains fresh data or not.
        /// Use <see cref="Readable(ReferenceChannel)"/> for this check.</remarks>
        public float[] RetrieveSamples(ReferenceChannel channel) {
            if ((int)channel < 8) {
                return output[(int)channel].lastSamples;
            }
            for (int i = 8; i < output.Length; ++i) {
                if (output[i].target == channel) {
                    return output[i].lastSamples;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the position of <paramref name="channel"/> in <see cref="output"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetChannelId(ReferenceChannel channel) {
            if ((int)channel < 8) {
                return (int)channel;
            }
            for (int i = 8; i < output.Length; ++i) {
                if (output[i].target == channel) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Initializes this upmixer by creating the obligatory reference channels and the extra channels that are
        /// used by the <see cref="source"/>.
        /// </summary>
        void Init() {
            ReferenceChannel[] layout = ChannelPrototype.GetStandardMatrix(8);
            output = new ChannelData[layout.Length];
            for (int channel = 0; channel < layout.Length; ++channel) {
                output[channel].target = layout[channel];
            }
            output[0].lastSamples = new float[0]; // To skip null check in GenerateSamples

            layout = ChannelPrototype.GetStandardMatrix(source.Channels);
            List<ReferenceChannel> missing = new List<ReferenceChannel>();
            for (int channel = 0; channel < layout.Length; ++channel) {
                if (!output.Has(entry => entry.target == layout[channel])) {
                    missing.Add(layout[channel]);
                }
            }

            int offset = output.Length, news = missing.Count;
            Array.Resize(ref output, offset + news);
            for (int channel = 0; channel < news; ++channel) {
                output[channel + offset] = new ChannelData(missing[channel]);
            }
        }
    }
}