﻿using System;
using System.Collections.ObjectModel;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Measures properties of a filter, like frequency/impulse response, gain, or delay.</summary>
    public sealed class FilterAnalyzer {
        /// <summary>Used FFT size for most measurements.</summary>
        const int resolution = 65536;
        /// <summary>First measured frequency.</summary>
        const double startFreq = 10;

        /// <summary>Maximum filter amplification.</summary>
        public float Gain {
            get {
                if (!float.IsNaN(gain))
                    return gain;
                gain = Spectrum[0];
                for (int i = 1; i < spectrum.Length; ++i)
                    if (gain < spectrum[i])
                        gain = spectrum[i];
                return gain;
            }
        }
        float gain = float.NaN;

        /// <summary><see cref="FFTCache"/> used for <see cref="FrequencyResponse"/>.</summary>
        FFTCache Cache {
            get {
                if (cache == null)
                    return cache = new FFTCache(resolution);
                return cache;
            }
        }
        FFTCache cache;

        /// <summary>Swept sine used for frequency and impulse response measurements.</summary>
        float[] ImpulseReference {
            get {
                if (impulseReference != null)
                    return impulseReference;
                impulseReference = new float[resolution];
                impulseReference[0] = 1;
                return impulseReference;
            }
        }
        float[] impulseReference;

        /// <summary>Frequency response of the filter.</summary>
        internal Complex[] FrequencyResponse {
            get {
                if (frequencyResponse == null)
                    return frequencyResponse = Measurements.FFT(ImpulseResponse, Cache);
                return frequencyResponse;
            }
        }
        Complex[] frequencyResponse;

        /// <summary>Absolute of <see cref="FrequencyResponse"/> up to half the sample rate.</summary>
        internal float[] Spectrum {
            get {
                if (spectrum == null)
                    return spectrum = Measurements.GetSpectrum(FrequencyResponse);
                return spectrum;
            }
        }
        float[] spectrum;

        /// <summary>Impulse response processor.</summary>
        VerboseImpulseResponse Impulse {
            get {
                if (impulse == null) {
                    float[] response = (float[])ImpulseReference.Clone();
                    filter.Process(response);
                    return impulse = new VerboseImpulseResponse(response);
                }
                return impulse;
            }
        }
        VerboseImpulseResponse impulse;

        /// <summary>Maximum filter amplification in decibels.</summary>
        public float GainDecibels => (float)(20 * Math.Log10(Gain));
        /// <summary>Filter impulse response samples.</summary>
        public float[] ImpulseResponse => Impulse.Response;
        /// <summary>Filter polarity, true if positive.</summary>
        public bool Polarity => Impulse.Polarity;
        /// <summary>Response delay in seconds.</summary>
        public float Delay => Impulse.Delay / (float)SampleRate;

        /// <summary>Sample rate used for measurements and in <see cref="filter"/> if it's sample rate-dependent.</summary>
        public int SampleRate { get; private set; }
        /// <summary>Filter to measure.</summary>
        Filter filter;

        /// <summary>Copy a filter for measurements.</summary>
        /// <param name="filter">Filter to measure</param>
        /// <param name="sampleRate">Sample rate used for measurements and in <paramref name="filter"/> if it's sample rate-dependent</param>
        public FilterAnalyzer(Filter filter, int sampleRate) {
            this.filter = filter;
            SampleRate = sampleRate;
        }

        /// <summary>Change the filter while keeping the sample rate.</summary>
        public void Reset(Filter filter) {
            this.filter = filter;
            gain = float.NaN;
            frequencyResponse = null;
            spectrum = null;
            impulse = null;
        }

        /// <summary>Change the filter and the sample rate.</summary>
        public void Reset(Filter filter, int sampleRate) {
            Reset(filter);
            if (SampleRate != sampleRate) {
                SampleRate = sampleRate;
                impulseReference = null;
            }
        }

        /// <summary>Fet the frequency response of the filter.</summary>
        public Complex[] GetFrequencyResponse() => (Complex[])FrequencyResponse.Clone();

        /// <summary>Fet the frequency response of the filter.</summary>
        public ReadOnlyCollection<Complex> GetFrequencyResponseReadonly() => Array.AsReadOnly(FrequencyResponse);

        /// <summary>Get the absolute of <see cref="FrequencyResponse"/> up to half the sample rate.</summary>
        public float[] GetSpectrum() => (float[])Spectrum.Clone();

        /// <summary>Get the absolute of <see cref="FrequencyResponse"/> up to half the sample rate.</summary>
        public ReadOnlyCollection<float> GetSpectrumReadonly() => Array.AsReadOnly(Spectrum);
    }
}