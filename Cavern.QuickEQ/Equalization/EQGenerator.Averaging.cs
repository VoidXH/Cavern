using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    public static partial class EQGenerator {
        /// <summary>
        /// Get the average gains of multiple equalizers. The averaging happens in linear space.
        /// </summary>
        /// <remarks>All <paramref name="sources"/> must have an equal number of bands at the same frequencies.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Equalizer Average(params Equalizer[] sources) {
            double mul = 1.0 / sources[0].Bands.Count;
            return Average(QMath.DbToGain, x => QMath.GainToDb(x * mul), sources);
        }

        /// <summary>
        /// Get the average gains of multiple equalizers. The averaging happens in linear space and an RMS value is taken.
        /// </summary>
        /// <remarks>All <paramref name="sources"/> must have an equal number of bands at the same frequencies.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Equalizer AverageRMS(params Equalizer[] sources) {
            double mul = 1.0 / sources.Length;
            return Average(RMSAddition, x => QMath.GainToDb(Math.Sqrt(x) * mul), sources);
        }

        /// <summary>
        /// Get the average gains of multiple equalizers, regardless of how many bands they have. The averaging happens in linear space.
        /// </summary>
        public static Equalizer AverageSafe(params Equalizer[] sources) {
            List<Band> bands = new List<Band>();
            double div = 1.0 / sources.Length;
            for (int i = 0; i < sources.Length; i++) {
                IReadOnlyList<Band> source = sources[i].Bands;
                for (int j = 0, c = source.Count; j < c; j++) {
                    double freq = source[j].Frequency,
                        gain = 0;
                    for (int other = 0; other < sources.Length; other++) {
                        gain += Math.Pow(10, sources[other][freq] * .05f);
                    }
                    bands.Add(new Band(freq, 20 * Math.Log10(gain * div)));
                }
            }

            bands.Sort();
            return new Equalizer(bands.Distinct().ToList(), true);
        }

        /// <summary>
        /// Get the maximum at each band for multiple equalizers.
        /// </summary>
        /// <remarks>All <paramref name="sources"/> must have an equal number of bands at the same frequencies.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Equalizer Max(params Equalizer[] sources) => MinMax(sources, false);

        /// <summary>
        /// Get the minimum at each band for multiple equalizers.
        /// </summary>
        /// <remarks>All <paramref name="sources"/> must have an equal number of bands at the same frequencies.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Equalizer Min(params Equalizer[] sources) => MinMax(sources, true);

        /// <summary>
        /// Get the average gains of multiple equalizers by a custom averaging function.
        /// </summary>
        /// <param name="addition">Transformation when a value is added to the average calculation</param>
        /// <param name="division">Transformation when the average from the accumulator is taken</param>
        /// <param name="sources">Curves to get the average of</param>
        /// <remarks>All <paramref name="sources"/> must have an equal number of bands at the same frequencies.</remarks>
        static Equalizer Average(Func<double, double> addition, Func<double, double> division, params Equalizer[] sources) {
            double[] bands = new double[sources[0].Bands.Count];
            IReadOnlyList<Band> source = sources[0].Bands;
            for (int i = 0; i < bands.Length; i++) {
                bands[i] = addition(source[i].Gain);
            }
            for (int i = 1; i < sources.Length; i++) {
                source = sources[i].Bands;
                for (int j = 0; j < bands.Length; j++) {
                    bands[j] += addition(source[j].Gain);
                }
            }

            List<Band> result = new List<Band>(bands.Length);
            for (int i = 0; i < bands.Length; i++) {
                result.Add(new Band(source[i].Frequency, division(bands[i])));
            }
            return new Equalizer(result, true);
        }

        /// <summary>
        /// Inner workings of <see cref="Min(Equalizer[])"/> and <see cref="Max(Equalizer[])"/>, as the only difference is the check.
        /// </summary>
        static Equalizer MinMax(Equalizer[] sources, bool min) {
            IReadOnlyList<Band>[] bands = sources.Select(x => x.Bands).ToArray();
            List<Band> output = new List<Band>();
            for (int i = 0, c = bands[0].Count; i < c; i++) {
                double gain = min ? double.MaxValue : double.MinValue;
                for (int j = 0; j < bands.Length; j++) {
                    if (min ? gain > bands[j][i].Gain : gain < bands[j][i].Gain) {
                        gain = bands[j][i].Gain;
                    }
                }
                output.Add(new Band(bands[0][i].Frequency, gain));
            }
            return new Equalizer(output, true);
        }

        /// <summary>
        /// Helper function for <see cref="AverageRMS(Equalizer[])"/>, adds a decibel value to the accumulator as a squared voltage.
        /// </summary>
        static double RMSAddition(double x) {
            x = QMath.DbToGain(x);
            return x * x;
        }
    }
}