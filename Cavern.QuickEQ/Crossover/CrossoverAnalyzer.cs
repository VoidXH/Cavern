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
                Complex[] lowCurrent = lowTransfer.FastClone();
                Complex[] highCurrent = highTransfer.FastClone();
                lowCurrent.Convolve(type.GetLowpass(sampleRate, freq, cache.Size).FFT(cache));
                highCurrent.Convolve(type.GetHighpass(sampleRate, freq, cache.Size).FFT(cache));
                lowCurrent.Add(highCurrent);
                float value = lowCurrent.GetRMSMagnitude();
                if (bestValue < value) {
                    bestValue = value;
                    bestFrequency = freq;
                }
            }
            return bestFrequency;
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
        static Complex[] GetCombinedLFETransfer(MeasurementPosition measurement, Channel[] channels) {
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
            return subs;
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
        public float[] FindCrossoverFrequencies(MeasurementPosition measurement, Channel[] channels, FFTCachePool pool) {
            channels ??= ChannelPrototype.ToLayout(ChannelPrototype.GetStandardMatrix(measurement.Length));
            Complex[] subs = GetCombinedLFETransfer(measurement, channels);
            if (subs == null) {
                return null;
            }

            subs.Gain(Crossover.minus10dB);
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