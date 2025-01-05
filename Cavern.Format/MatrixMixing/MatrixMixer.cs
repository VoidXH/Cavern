using Cavern.Filters;
using Cavern.Format.Common;
using Cavern.Utilities;

namespace Cavern.Format.MatrixMixing {
    /// <summary>
    /// Encodes frames of multichannel audio data to less channels, then back.
    /// </summary>
    public class MatrixMixer {
        /// <summary>
        /// For each encoded channel, the contribution of each source channel.
        /// </summary>
        readonly Filter[][] encoders;

        /// <summary>
        /// For each decoded channel, the contribution of each encoded channel.
        /// </summary>
        readonly Filter[][] decoders;

        /// <summary>
        /// Encodes frames of multichannel audio data to less channels, then back.
        /// The complex numbers in the matrices can be one of 4:<br />
        /// - 0: no mixing will happen.<br />
        /// - Real: mixed with this gain.<br />
        /// - Positive imaginary: mixed with this gain and a 90-degree phase shift.<br />
        /// - Negative imaginary: mixed with this gain and a -90-degree phase shift.
        /// </summary>
        /// <param name="encodingMatrix">For each encoded channel, the contribution of each source channel</param>
        /// <param name="decodingMatrix">For each decoded channel, the contribution of each encoded channel</param>
        /// <param name="blockSize">Length of the filters</param>
        public MatrixMixer(Complex[][] encodingMatrix, Complex[][] decodingMatrix, int blockSize) {
            encoders = ConvertMatrixToFilters(encodingMatrix, blockSize, true);
            decoders = ConvertMatrixToFilters(decodingMatrix, blockSize, false);
        }

        /// <summary>
        /// Create the <see cref="encoders"/> or <see cref="decoders"/>.
        /// </summary>
        /// <param name="matrix">Input encoding or decoding matrix</param>
        /// <param name="blockSize">Length of the filters</param>
        /// <param name="forward">Create an encoder instead of a decoder</param>
        static Filter[][] ConvertMatrixToFilters(Complex[][] matrix, int blockSize, bool forward) {
            Filter[][] result = new Filter[matrix.Length][];
            for (int i = 0; i < matrix.Length; i++) {
                Complex[] source = matrix[i];
                Filter[] target = result[i] = new Filter[source.Length];
                for (int j = 0; j < source.Length; j++) {
                    if (source[j].Real != 0 && source[j].Imaginary == 0) {
                        target[j] = new Gain(source[j].Real);
                    } else if (source[j].Real == 0 && source[j].Imaginary != 0) {
                        bool actualForward = forward;
                        if (source[j].Imaginary < 0) {
                            actualForward = !actualForward;
                            source[j].Imaginary = -source[j].Imaginary;
                        }
                        target[j] = new ComplexFilter(
                            new PhaseShifter(blockSize, actualForward),
                            new Gain(actualForward ? source[j].Imaginary : -source[j].Imaginary)
                        );
                    } else if (source[j].Real != 0 && source[j].Imaginary != 0) {
                        throw new ComplexNumberFilledException();
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Perform encoding or decoding using the transform matrix of the desired transformation.
        /// </summary>
        static void Process(MultichannelWaveform source, MultichannelWaveform target, Filter[][] transform) {
            float[] working = new float[source.Length];
            for (int t = 0; t < target.Channels; t++) {
                target[t].Clear();
                Filter[] channelCoding = transform[t];
                for (int s = 0; s < source.Channels; s++) {
                    if (channelCoding[s] != null) {
                        source[s].CopyTo(working);
                        channelCoding[s].Process(working);
                        WaveformUtils.Mix(working, target[t]);
                    }
                }
            }
        }

        /// <summary>
        /// Encode the <paramref name="source"/> to the <paramref name="target"/> using the encoding matrix.
        /// </summary>
        public void Encode(MultichannelWaveform source, MultichannelWaveform target) => Process(source, target, encoders);

        /// <summary>
        /// Decode the <paramref name="source"/> to the <paramref name="target"/> using the decoding matrix.
        /// </summary>
        public void Decode(MultichannelWaveform source, MultichannelWaveform target) => Process(source, target, decoders);
    }
}