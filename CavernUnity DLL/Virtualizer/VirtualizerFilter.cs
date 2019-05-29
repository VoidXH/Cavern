using System;
using System.Threading.Tasks;

using Cavern.Filters;

namespace Cavern.Virtualizer {
    /// <summary>Convolution filters for each ear and virtual channel to simulate a spatial environment.</summary>
    static partial class VirtualizerFilter {
        const int FilterSampleRate = 48000;
        /// <summary>Cache of each output channel.</summary>
        static float[][] OriginalSplit;
        /// <summary>Cache of each output channel for one ear.</summary>
        static float[][] LeftSplit, RightSplit;

        /// <summary>Set up virtual channel set for the virtualization filters.</summary>
        public static void SetLayout() {
            int ChannelCount = SpatialChannels.Length, UpdateRate = AudioListener3D.Current.UpdateRate;
            ChannelUnity[] NewChannels = new ChannelUnity[ChannelCount];
            for (int i = 0; i < ChannelCount; ++i)
                NewChannels[i] = new ChannelUnity(SpatialChannels[i].X, SpatialChannels[i].Y);
            AudioListener3D.Channels = NewChannels;
            if (OriginalSplit == null) {
                OriginalSplit = new float[ChannelCount][];
                LeftSplit = new float[ChannelCount][];
                RightSplit = new float[ChannelCount][];
            }
            if (OriginalSplit[0] == null || OriginalSplit[0].Length != UpdateRate) {
                for (int Channel = 0; Channel < ChannelCount; ++Channel) {
                    OriginalSplit[Channel] = new float[UpdateRate];
                    LeftSplit[Channel] = new float[UpdateRate];
                    RightSplit[Channel] = new float[UpdateRate];
                }
            }
        }

        /// <summary>Apply the virtualizer on the <see cref="AudioListener3D"/>'s output,
        /// if the configuration matches the virtualization layout and filter sample rate.</summary>
        public static void Process(float[] Output) {
            int ChannelCount = AudioListener3D.ChannelCount, UpdateRate = AudioListener3D.Current.UpdateRate, BlockCopySize = UpdateRate * sizeof(float);
            if (AudioListener3D.Current.SampleRate != FilterSampleRate)
                return;
            int OutputSample = 0;
            for (int Sample = 0; Sample < UpdateRate; ++Sample)
                for (int Channel = 0; Channel < ChannelCount; ++Channel)
                    OriginalSplit[Channel][Sample] = Output[OutputSample++];
            // Convolution
            Parallel.For(0, ChannelCount, Channel => {
                // Select the retain range
                Crossover LowCrossover = SpatialChannels[Channel].LowCrossover, HighCrossover = SpatialChannels[Channel].HighCrossover;
                LowCrossover.Process(OriginalSplit[Channel]);
                SpatialChannels[Channel].HighCrossover.Process(LowCrossover.HighOutput);
                OriginalSplit[Channel] = LowCrossover.LowOutput;
                for (int Sample = 0; Sample < UpdateRate; ++Sample)
                    OriginalSplit[Channel][Sample] += HighCrossover.HighOutput[Sample];
                // Select the impulse response frequency range
                LeftSplit[Channel] = HighCrossover.LowOutput;
                Buffer.BlockCopy(LeftSplit[Channel], 0, RightSplit[Channel], 0, BlockCopySize);
                SpatialChannels[Channel].LeftFilter.Process(LeftSplit[Channel]);
                SpatialChannels[Channel].RightFilter.Process(RightSplit[Channel]);
            });
            // Stereo downmix
            Array.Clear(Output, 0, Output.Length);
            for (int Sample = 0; Sample < UpdateRate; ++Sample) {
                int LeftOut = Sample * ChannelCount, RightOut = LeftOut + 1;
                for (int Channel = 0; Channel < ChannelCount; ++Channel) {
                    float Unspatialized = OriginalSplit[Channel][Sample] * .5f;
                    Output[LeftOut] += LeftSplit[Channel][Sample] + Unspatialized;
                    Output[RightOut] += RightSplit[Channel][Sample] + Unspatialized;
                }
            }
        }
    }
}