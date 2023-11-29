using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

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
            int inputs = playedContent.Length,
                outputs = usedLayout.Length;

            // Create simulation
            Listener simulator = new Listener(false) {
                UpdateRate = Math.Max(inputs, 16),
                LFESeparation = true,
                DirectLFE = true
            };
            Channel[] oldChannels = Listener.Channels;
            Listener.ReplaceChannels(usedLayout);
            for (int i = 0; i < inputs; i++) {
                simulator.AttachSource(new Source {
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
        /// Get a mixing matrix that maps the <paramref name="playedContent"/> to the user-set <see cref="Listener.Channels"/> of the
        /// current system. The result is a set of multipliers for each output (playback) channel, with which the input (content)
        /// channels should be multiplied and mixed to that specific channel. The dimensions are [output channels][input channels].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[][] GetMatrix(Channel[] playedContent) => GetMatrix(playedContent, Listener.Channels);

        /// <summary>
        /// Convert a spatial remapping matrix to an Equalizer APO Copy filter.
        /// </summary>
        public static string ToEqualizerAPO(float[][] matrix) {
            StringBuilder result = new StringBuilder("Copy:");
            for (int i = 0; i < matrix.Length; i++) {
                float[] input = matrix[i];
                bool started = false;
                string label = EqualizerAPOUtils.GetChannelLabel(i, matrix.Length);
                for (int j = 0; j < input.Length; j++) {
                    if (input[j] != 0) {
                        if (!started) {
                            result.Append(' ').Append(label).Append('=');
                            started = true;
                        } else {
                            result.Append('+');
                        }
                        if (input[j] != 1) {
                            result.Append(input[j].ToString(CultureInfo.InvariantCulture)).Append('*');
                        }
                        result.Append(EqualizerAPOUtils.GetChannelLabel(j, input.Length));
                    }
                }
                if (!started) {
                    result.Append(' ').Append(label).Append("=0");
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Create an Equalizer APO Copy filter that matrix mixes the <paramref name="playedContent"/> to the user-set
        /// <see cref="Listener.Channels"/> of the current system.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToEqualizerAPO(Channel[] playedContent) => ToEqualizerAPO(GetMatrix(playedContent));

        /// <summary>
        /// Convert a spatial remapping matrix to an XML file.
        /// </summary>
        /// <param name="matrix">A mixing matrix created with one of the <see cref="GetMatrix(Channel[])"/> functions</param>
        /// <param name="extraParams">Optional argument keys and their values for all output channels - if the number of the
        /// values is less than the number of channels, the last channels without values will skip that attribute</param>
        public static string ToXML(float[][] matrix, params (string key, string[] values)[] extraParams) {
            StringBuilder result = new StringBuilder();
            using XmlWriter writer = XmlWriter.Create(result);
            writer.WriteStartElement("matrix");
            for (int output = 0; output < matrix.Length; output++) {
                float[] inputs = matrix[output];
                writer.WriteStartElement("output");
                writer.WriteAttributeString("channel", output.ToString());
                for (int extra = 0; extra < extraParams.Length; extra++) {
                    (string key, string[] values) = extraParams[extra];
                    if (output < values.Length) {
                        writer.WriteAttributeString(key, values[output].ToString());
                    }
                }
                for (int input = 0; input < inputs.Length; input++) {
                    if (inputs[input] != 0) {
                        writer.WriteStartElement("input");
                        writer.WriteAttributeString("channel", input.ToString());
                        writer.WriteAttributeString("gain", inputs[input].ToString(CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.Flush();
            return result.ToString();
        }

        /// <summary>
        /// Create an XML file that describes a matrix mix that mixes the <paramref name="playedContent"/> to the user-set
        /// <see cref="Listener.Channels"/> of the current system.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToXML(Channel[] playedContent) => ToXML(GetMatrix(playedContent));

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