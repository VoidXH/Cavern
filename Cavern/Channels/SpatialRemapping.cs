using System;

using Cavern.Utilities;

namespace Cavern.Channels {
    /// <summary>
    /// Multiple ways of getting a mixing matrix to simulate one channel layout on a different one. While Cavern can play any standard
    /// content on any layout, getting the matrix is useful for applying this feature in calibrations.
    /// </summary>
    public static class SpatialRemapping {
        /// <summary>
        /// Get a mixing matrix that maps the <paramref name="playedContent"/> to a <paramref name="usedLayout"/>. The result is a set of
        /// multipliers for each output (playback) channel, with which the input (content) channels should be multiplied and mixed to that
        /// specific channel. The dimensions are [output channels][input channels].
        /// </summary>
        public static float[][] GetMatrix(Channel[] playedContent, Channel[] usedLayout) {
            Channel[] oldChannels = Listener.Channels;
            Listener.ReplaceChannels(usedLayout);
            int inputs = playedContent.Length,
                outputs = usedLayout.Length;

            // Create simulation
            Listener simulator = new Listener() {
                UpdateRate = Math.Max(inputs, 16),
                LFESeparation = true,
                DirectLFE = true
            };
            for (int i = 0; i < inputs; i++) {
                simulator.AttachSource(new Source() {
                    Clip = GetClipForChannel(i, inputs, simulator.SampleRate),
                    Position = playedContent[i].SpatialPos * Listener.EnvironmentSize,
                    LFE = playedContent[i].LFE,
                    VolumeRolloff = Rolloffs.Disabled
                });
            }

            // Simulate and format
            float[] result = simulator.Render();
            Listener.ReplaceChannels(oldChannels);
            int expectedLength = inputs * outputs;
            if (result.Length > expectedLength) {
                Array.Resize(ref result, expectedLength);
            }
            float[][] output = new float[outputs][];
            for (int i = 0; i < outputs; i++) {
                output[i] = new float[inputs];
            }
            WaveformUtils.InterlacedToMultichannel(result, output);
            return output;
        }

        /// <summary>
        /// Create a <see cref="Clip"/> that is 1 at the channel's index and 0 everywhere else.
        /// </summary>
        static Clip GetClipForChannel(int channel, int channels, int sampleRate) {
            float[] data = new float[channels];
            data[channel] = 1;
            return new Clip(data, 1, sampleRate);
        }
    }
}