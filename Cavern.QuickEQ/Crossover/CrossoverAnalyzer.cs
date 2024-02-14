using System;

using Cavern.Channels;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Tools for analyzing and tuning measured or generated crossovers.
    /// </summary>
    public class CrossoverAnalyzer {
        /// <summary>
        /// An instance of the used crossover type.
        /// </summary>
        public Crossover type;

        /// <summary>
        /// The sample rate of recorded or simulated transfer functions of each channel that will be analyzed.
        /// </summary>
        public int sampleRate;

        /// <summary>
        /// The lowest possible resulting crossover frequency.
        /// </summary>
        public float minFreq = 40;

        /// <summary>
        /// The highest possible resulting crossover frequency.
        /// </summary>
        public float maxFreq = 100;

        /// <summary>
        /// Steps between checked crossover frequencies.
        /// </summary>
        public float precision = 10;

        /// <summary>
        /// A multichannel crossover analyzer instance.
        /// </summary>
        /// <param name="type">An instance of the used crossover type</param>
        /// <param name="sampleRate">The sample rate of recorded or simulated transfer functions of each channel
        /// that will be analyzed</param>
        public CrossoverAnalyzer(Crossover type, int sampleRate) {
            this.type = type;
            this.sampleRate = sampleRate;
        }

        /// <summary>
        /// Gets the optimal frequency to put the crossover point at for a single channel by simulation.
        /// </summary>
        /// <param name="type">An instance of the used crossover type</param>
        /// <param name="lowTransfer">Transfer function of the low-frequency path</param>
        /// <param name="highTransfer">Transfer function of the high-frequency path</param>
        /// <param name="sampleRate">Sample rate where the transfer functions were recorded</param>
        /// <param name="minFreq">Minimum allowed crossover frequency</param>
        /// <param name="maxFreq">Maximum allowed crossover frequency</param>
        /// <param name="precision">Steps between checked crossover frequencies</param>
        /// <remarks>This function doesn't account for the 10 dB gain of LFE channels as it could be used for determining the
        /// crossover point of multiway speakers too.</remarks>
        public static float FindCrossoverFrequency(Crossover type, Complex[] lowTransfer, Complex[] highTransfer, int sampleRate,
            float minFreq, float maxFreq, float precision) {
            using FFTCache cache = new ThreadSafeFFTCache(lowTransfer.Length);
            return FindCrossoverFrequency(type, lowTransfer, highTransfer, sampleRate, minFreq, maxFreq, precision, cache);
        }

        /// <summary>
        /// Gets the optimal frequency to put the crossover point at for a single channel by simulation.
        /// </summary>
        /// <param name="type">An instance of the used crossover type</param>
        /// <param name="lowTransfer">Transfer function of the low-frequency path</param>
        /// <param name="highTransfer">Transfer function of the high-frequency path</param>
        /// <param name="sampleRate">Sample rate where the transfer functions were recorded</param>
        /// <param name="minFreq">Minimum allowed crossover frequency</param>
        /// <param name="maxFreq">Maximum allowed crossover frequency</param>
        /// <param name="precision">Steps between checked crossover frequencies</param>
        /// <param name="cache">Preallocated FFT cache for optimization</param>
        /// <remarks>This function doesn't account for the 10 dB gain of LFE channels as it could be used for determining the
        /// crossover point of multiway speakers too.</remarks>
        public static float FindCrossoverFrequency(Crossover type, Complex[] lowTransfer, Complex[] highTransfer, int sampleRate,
            float minFreq, float maxFreq, float precision, FFTCache cache) {
            float bestValue = 0,
                bestFrequency = 0;
            for (float freq = minFreq; freq <= maxFreq; freq += precision) {
                float value = GetCrossoverValue(type, lowTransfer, highTransfer, sampleRate, freq, cache);
                if (bestValue < value) {
                    bestValue = value;
                    bestFrequency = freq;
                }
            }
            return bestFrequency;
        }

        /// <summary>
        /// Find the lowest crossover point on a system by comparing all channels to each other and
        /// checking until what frequency they are similar.
        /// </summary>
        /// <param name="channels">Transfer functions of each channel - lengths must match</param>
        /// <param name="refChannel">The channel index of where crossovered bass goes</param>
        /// <param name="sampleRate">Measurement sample rate</param>
        /// <param name="minFreq">Minimum checked frequency</param>
        /// <param name="allowedError">Ratio of change in spectrum between channels is allowed to consider them the same</param>
        public static float FindExistingCrossover(Complex[][] channels, int refChannel, int sampleRate, float minFreq, float allowedError) {
            allowedError *= allowedError; // Work with squared magnitudes
            Complex[] reference = channels[refChannel];
            float[] work = new float[reference.Length];
            int checkedFrom = (int)(minFreq / sampleRate * work.Length);
            int minBand = work.Length >> 1;
            for (int i = 0; i < channels.Length; i++) {
                if (i == refChannel) {
                    continue;
                }
                Complex[] other = channels[i];
                for (int j = checkedFrom; j < minBand; j++) { // Convert to squared deconvolution magnitude
                    work[j] = (reference[j] * (1 / other[j].Magnitude)).SqrMagnitude;
                }
                for (int j = 0; j < checkedFrom; j++) {
                    work[j] = work[checkedFrom]; // So smoothing won't cause issues
                }
                work = GraphUtils.SmoothUniform(work, Math.Max(reference.Length * 10 / sampleRate, 1));
                float upperError = allowedError * work[checkedFrom],
                    lowerError = 1 / allowedError * work[checkedFrom];
                for (int band = checkedFrom; band < minBand; band++) {
                    if (work[band] > upperError || work[band] < lowerError) {
                        if (minBand > band) {
                            minBand = band;
                        }
                        break;
                    }
                }
            }
            return (float)minBand / work.Length * sampleRate;
        }

        /// <summary>
        /// Group the channels by a binary function (for example, screen and surround channels), and get their average crossover
        /// frequencies or 0 if a group has no channels.
        /// </summary>
        /// <param name="frequencies">Crossover points for all channels, could be generated with
        /// <see cref="FindCrossoverFrequencies(MeasurementPosition, Channel[])"/></param>
        /// <param name="channels">The channel layout where the measurement was taken,
        /// or null if unknown - in that case, a reference layout with that channel count will be used</param>
        /// <param name="selector">The function that results true for channel indexes in the group</param>
        /// <remarks>LFE channels are not considered in any group.</remarks>
        public static (float inGroup, float outGroup) Group(float[] frequencies, Channel[] channels, Func<int, bool> selector) {
            channels ??= ChannelPrototype.ToLayout(ChannelPrototype.GetStandardMatrix(frequencies.Length));
            int inCount = 0,
                outCount = 0;
            float inTotal = 0,
                outTotal = 0;
            for (int i = 0; i < frequencies.Length; i++) {
                if (!channels[i].LFE) {
                    if (selector(i)) {
                        inCount++;
                        inTotal += frequencies[i];
                    } else {
                        outCount++;
                        outTotal += frequencies[i];
                    }
                }
            }
            if (inCount != 0) {
                inTotal /= inCount;
            }
            if (outCount != 0) {
                outTotal /= outCount;
            }
            return (inTotal, outTotal);
        }

        /// <summary>
        /// Get the transfer function of all LFE channels playing together.
        /// </summary>
        /// <param name="measurement">Measurements or simulations at the reference position</param>
        /// <param name="channels">The channel layout where the <paramref name="measurement"/> was taken</param>
        /// <remarks>This function does account for the 10 dB gain of LFE channels.</remarks>
        protected static Complex[] GetCombinedLFETransfer(MeasurementPosition measurement, Channel[] channels) {
            Complex[] subs = null;
            for (int i = 0; i < channels.Length; i++) {
                if (channels[i].LFE) {
                    if (subs == null) {
                        subs = (Complex[])measurement[i].Clone();
                    } else {
                        subs.Add(measurement[i]);
                    }
                }
            }

            subs?.Gain(Crossover.minus10dB);
            return subs;
        }

        /// <summary>
        /// Calculate a single crossover point's score for comparing it to others.
        /// </summary>
        /// <param name="type">An instance of the used crossover type</param>
        /// <param name="lowTransfer">Transfer function of the low-frequency path</param>
        /// <param name="highTransfer">Transfer function of the high-frequency path</param>
        /// <param name="sampleRate">Sample rate where the transfer functions were recorded</param>
        /// <param name="freq">Crossover point frequency to score</param>
        /// <param name="cache">Preallocated FFT cache for optimization</param>
        protected static float GetCrossoverValue(Crossover type, Complex[] lowTransfer, Complex[] highTransfer, int sampleRate, float freq,
            FFTCache cache) {
            Complex[] lowCurrent = lowTransfer.FastClone();
            Complex[] highCurrent = highTransfer.FastClone();
            Complex[] work = new Complex[lowCurrent.Length];

            type.GetLowpass(sampleRate, freq, cache.Size).ParseForFFT(work);
            work.InPlaceFFT(cache);
            lowCurrent.Convolve(work);

            type.GetHighpass(sampleRate, freq, cache.Size).ParseForFFT(work);
            work.InPlaceFFT(cache);
            highCurrent.Convolve(work);

            lowCurrent.Add(highCurrent);
            return lowCurrent.GetRMSMagnitude();
        }

        /// <summary>
        /// Gets the optimal frequencies to put the crossover point at for each channel by simulation.
        /// </summary>
        /// <param name="measurement">Measurements or simulations at the reference position</param>
        /// <param name="channels">The channel layout where the <paramref name="measurement"/> was taken,
        /// or null if unknown - in that case, a reference layout with that channel count will be used</param>
        /// <returns>The frequencies where each channel's crossover should be positioned or null if
        /// bypassing the crossover is recommended</returns>
        /// <remarks>This function does account for the 10 dB gain of LFE channels.</remarks>
        public float[] FindCrossoverFrequencies(MeasurementPosition measurement, Channel[] channels) {
            using FFTCachePool pool = new FFTCachePool(measurement[0].Length);
            return FindCrossoverFrequencies(measurement, channels, pool);
        }

        /// <summary>
        /// Gets the optimal frequencies to put the crossover point at for each channel by simulation.
        /// </summary>
        /// <param name="measurement">Measurements or simulations at the reference position</param>
        /// <param name="channels">The channel layout where the <paramref name="measurement"/> was taken,
        /// or null if unknown - in that case, a reference layout with that channel count will be used</param>
        /// <param name="pool">Preallocated FFT cache pool</param>
        /// <returns>The frequencies where each channel's crossover should be positioned or null if
        /// bypassing the crossover is recommended</returns>
        /// <remarks>This function does account for the 10 dB gain of LFE channels.</remarks>
        public virtual float[] FindCrossoverFrequencies(MeasurementPosition measurement, Channel[] channels, FFTCachePool pool) {
            channels ??= ChannelPrototype.ToLayout(ChannelPrototype.GetStandardMatrix(measurement.Length));
            Complex[] subs = GetCombinedLFETransfer(measurement, channels);
            if (subs == null) {
                return null;
            }

            float[] result = new float[channels.Length];
            Parallelizer.For(0, channels.Length, i => {
                if (!channels[i].LFE) {
                    FFTCache cache = pool.Lease();
                    result[i] = FindCrossoverFrequency(type, subs, measurement[i], sampleRate, minFreq, maxFreq, precision, cache);
                    pool.Return(cache);
                }
            });
            return result;
        }
    }
}