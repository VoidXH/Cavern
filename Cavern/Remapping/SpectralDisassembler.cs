using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Utilities;

using System;

namespace Cavern.Remapping {
    /// <summary>
    /// Deconstructs a stereo track to positioned bands. Bands will be created in equal octave ranges.
    /// </summary>
    class SpectralDisassembler {
        /// <summary>
        /// Lowest frequency to consider when creating any band.
        /// </summary>
        public double minFreq = 200;

        /// <summary>
        /// Highest frequency to consider when creating any band.
        /// </summary>
        public double maxFreq = 16000;

        /// <summary>
        /// Smoothness of object movements, [0;1], should be set from another [0;1] ratio
        /// with <see cref="Cavernize.CalculateSmoothingFactor(int, int, float)"/>.
        /// </summary>
        public float smoothnessFactor = 1;

        /// <summary>
        /// Contains one band of deconstruction, a fixed width of the full content spectrum, with relative spatial placement.
        /// </summary>
        public struct SpectralPart : IEquatable<SpectralPart> {
            /// <summary>
            /// Ratio of the position between left and right input channels.
            /// </summary>
            public float panning;

            /// <summary>
            /// Band-limited audio data from the mono mix of the original content.
            /// </summary>
            public float[] samples;

            /// Check if two <see cref="SpectralPart"/>s describe the same <see cref="panning"/>.
            bool IEquatable<SpectralPart>.Equals(SpectralPart other) => panning == other.panning;
        }

        /// <summary>
        /// Filters for each subband, twice (for each channel).
        /// </summary>
        readonly Filter[][] filters;

        /// <summary>
        /// Deconstructed content with bands and their positions.
        /// </summary>
        readonly SpectralPart[] output;

        /// <summary>
        /// Helper array used for mixing band-limited content together.
        /// </summary>
        float[] mixTemp = new float[0];

        /// <summary>
        /// Deconstructs a stereo track to positioned bands.
        /// </summary>
        /// <param name="bands">Number of bands to separate</param>
        /// <param name="sampleRate">Sample rate of the content to be filtered</param>
        public SpectralDisassembler(int bands, int sampleRate) {
            output = new SpectralPart[bands];
            filters = new Filter[bands][];

            double lastBand = minFreq,
                step = Math.Pow(10, (Math.Log10(maxFreq) - Math.Log10(minFreq)) / (bands - 1));
            filters[0] = new Filter[] {
                new ComplexFilter(new Lowpass(sampleRate, minFreq), new Lowpass(sampleRate, minFreq)),
                new ComplexFilter(new Lowpass(sampleRate, minFreq), new Lowpass(sampleRate, minFreq))
            };
            for (int i = 1; i < bands - 1; i++) {
                double nextBand = lastBand * step;
                filters[i] = new Filter[] {
                    new BandpassFlat(lastBand, nextBand, sampleRate, QFactor.reference, 2),
                    new BandpassFlat(lastBand, nextBand, sampleRate, QFactor.reference, 2)
                };
                lastBand = nextBand;
            }
            filters[^1] = new Filter[] {
                new ComplexFilter(new Highpass(sampleRate, lastBand), new Highpass(sampleRate, lastBand)),
                new ComplexFilter(new Highpass(sampleRate, lastBand), new Highpass(sampleRate, lastBand))
            };
        }

        /// <summary>
        /// Deconstruct a frame of stereo data to bands positioned between the source channels.
        /// </summary>
        public SpectralPart[] Process(float[] left, float[] right) {
            if (mixTemp.Length != left.Length) {
                mixTemp = new float[left.Length];
                for (int i = 0; i < output.Length; i++) {
                    output[i].samples = new float[left.Length];
                    output[i].panning = .5f;
                }
            }

            for (int i = 0; i < output.Length; i++) {
                Array.Copy(left, mixTemp, left.Length);
                Array.Copy(right, output[i].samples, right.Length);
                filters[i][0].Process(mixTemp);
                filters[i][1].Process(output[i].samples);
                float leftGain = WaveformUtils.GetRMS(mixTemp),
                    rightGain = WaveformUtils.GetRMS(output[i].samples),
                    newPanning = leftGain < rightGain ?
                        .5f + Math.Min(rightGain / Math.Max(leftGain, float.Epsilon) * .05f, .5f) :
                        (.5f - Math.Min(leftGain / Math.Max(rightGain, float.Epsilon) * .05f, .5f));
                output[i].panning = QMath.Lerp(output[i].panning, newPanning, smoothnessFactor);
                WaveformUtils.Mix(mixTemp, output[i].samples);
            }

            return output;
        }
    }
}