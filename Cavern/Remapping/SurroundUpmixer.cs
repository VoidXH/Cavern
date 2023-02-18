using System;
using System.Numerics;

using Cavern.Channels;
using Cavern.Utilities;

namespace Cavern.Remapping {
    /// <summary>
    /// Creates 5.1 or 7.1 mixes from any legacy stream by matrix upmixing. Keeps any additional channels.
    /// </summary>
    public class SurroundUpmixer : Upmixer {
        /// <summary>
        /// Which input channel should be routed to which output without any modification.
        /// </summary>
        readonly int[] sourceRouting;

        /// <summary>
        /// The front channels are available and matrixing can commence.
        /// </summary>
        readonly bool frontsAvailable;

        /// <summary>
        /// The center channel and LFE are available and no matrixing is needed for them.
        /// </summary>
        readonly bool centerAvailable;

        /// <summary>
        /// The side channels are available and no matrixing is needed for them.
        /// </summary>
        readonly bool sidesAvailable;

        /// <summary>
        /// The rear channels are available and no matrixing is needed for them.
        /// </summary>
        readonly bool rearsAvailable;

        /// <summary>
        /// Upmix up to 5.1 only.
        /// </summary>
        readonly bool mode51;

        /// <summary>
        /// Creates 7.1 mixes from any legacy stream by matrix upmixing. Keeps any additional channels, and
        /// uses Cavern's channel positions.
        /// </summary>
        /// <param name="sourceChannels">The channel that is present at each index in the input array</param>
        /// <param name="sampleRate">Content sample rate</param>
        public SurroundUpmixer(ReferenceChannel[] sourceChannels, int sampleRate) : this(sourceChannels, sampleRate, false, false) { }

        /// <summary>
        /// Creates 5.1 or 7.1 mixes from any legacy stream by matrix upmixing. Keeps any additional channels.
        /// </summary>
        /// <param name="sourceChannels">The channel that is present at each index in the input array</param>
        /// <param name="sampleRate">Content sample rate</param>
        /// <param name="mode51">Upmix up to 5.1 only, and don't create the rear channels</param>
        /// <param name="widen">Use the corners of the room for speaker placements
        /// instead of Cavern's internal positions (&quot;movie mode&quot;)</param>
        public SurroundUpmixer(ReferenceChannel[] sourceChannels, int sampleRate, bool mode51, bool widen) :
            base(matrixSize, sampleRate) {
            sourceRouting = new int[sourceChannels.Length];
            this.mode51 = mode51;

            bool[] bedAvailability = new bool[matrixSize];
            ReferenceChannel[] matrix = ChannelPrototype.GetStandardMatrix(matrixSize);
            for (int i = 0; i < sourceChannels.Length; i++) {
                sourceRouting[i] = -1;
                for (int j = 0; j < matrix.Length; j++) {
                    if (sourceChannels[i] == matrix[j]) {
                        if ((int)matrix[j] < matrixSize) {
                            bedAvailability[(int)matrix[j]] = true;
                        }
                        sourceRouting[i] = j;
                        break;
                    }
                }
            }

            frontsAvailable = bedAvailability[(int)ReferenceChannel.FrontLeft] && bedAvailability[(int)ReferenceChannel.FrontRight];
            centerAvailable = bedAvailability[(int)ReferenceChannel.FrontCenter] && bedAvailability[(int)ReferenceChannel.ScreenLFE];
            sidesAvailable = bedAvailability[(int)ReferenceChannel.SideLeft] && bedAvailability[(int)ReferenceChannel.SideRight];
            rearsAvailable = bedAvailability[(int)ReferenceChannel.RearLeft] && bedAvailability[(int)ReferenceChannel.RearRight];

            Vector3[] positions = widen ? ChannelPrototype.ToAlternativePositions(matrix) : ChannelPrototype.ToPositions(matrix);
            if (mode51) {
                positions[(int)ReferenceChannel.SideLeft] = positions[(int)ReferenceChannel.RearLeft];
                positions[(int)ReferenceChannel.SideRight] = positions[(int)ReferenceChannel.RearRight];
            }
            for (int i = 0; i < positions.Length; i++) {
                IntermediateSources[i].Position = positions[i];
            }
            IntermediateSources[(int)ReferenceChannel.ScreenLFE].LFE = true;
        }

        /// <summary>
        /// Get the input samples, place the upmixed targets in space, and return their samples.
        /// </summary>
        protected override float[][] UpdateSources(int samplesPerSource) {
            float[][] input = GetNewSamples(samplesPerSource);
            for (int i = 0; i < input.Length; i++) {
                if (sourceRouting[i] != -1) {
                    Array.Copy(input[i], output[sourceRouting[i]], input[i].Length);
                }
            }

            if (!frontsAvailable) {
                return output;
            }

            if (!centerAvailable) {
                MonoMixOf2(ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter);
            }

            if (!sidesAvailable) {
                if (rearsAvailable) {
                    MonoMixOf2(ReferenceChannel.FrontLeft, ReferenceChannel.RearLeft, ReferenceChannel.SideLeft);
                    MonoMixOf2(ReferenceChannel.FrontRight, ReferenceChannel.RearRight, ReferenceChannel.SideRight);
                } else {
                    DifferenceOf2(ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.SideLeft);
                    DifferenceOf2(ReferenceChannel.FrontRight, ReferenceChannel.FrontLeft, ReferenceChannel.SideRight);
                }
            }

            if (!rearsAvailable && !mode51) {
                WaveformUtils.Insert(output[(int)ReferenceChannel.SideLeft], output[(int)ReferenceChannel.RearLeft], .5f);
                WaveformUtils.Insert(output[(int)ReferenceChannel.SideRight], output[(int)ReferenceChannel.RearRight], .5f);
            }

            return output;
        }

        /// <summary>
        /// Mix a channel from two others by the (<paramref name="source1"/> + <paramref name="source2"/>) / 2 formula.
        /// </summary>
        void MonoMixOf2(ReferenceChannel source1, ReferenceChannel source2, ReferenceChannel target) {
            float[] targetArray = output[(int)target];
            output[(int)source1].CopyTo(targetArray);
            WaveformUtils.Mix(output[(int)source2], targetArray);
            WaveformUtils.Gain(targetArray, .5f);
        }

        /// <summary>
        /// Mix a channel from two others by the (<paramref name="source1"/> - <paramref name="source2"/>) / 2 formula.
        /// </summary>
        void DifferenceOf2(ReferenceChannel source1, ReferenceChannel source2, ReferenceChannel target) {
            float[] targetArray = output[(int)target];
            output[(int)source1].CopyTo(targetArray);
            output[(int)source2].Subtract(targetArray);
            WaveformUtils.Gain(targetArray, .5f);
        }

        /// <summary>
        /// Number of output channels, the corresponding standard matrix will be used.
        /// </summary>
        /// <remarks>Some values are hardcoded, this must always be 8.</remarks>
        public static readonly int matrixSize = 8;
    }
}