using System;
using System.Collections.ObjectModel;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Measures properties of a filter, like frequency/impulse response, gain, or delay.</summary>
    public sealed class FilterAnalyzer {
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

        /// <summary>Maximum filter amplification in decibels.</summary>
        public float GainDecibels => (float)(20 * Math.Log10(Gain));
        /// <summary>Filter impulse response samples.</summary>
        public float[] ImpulseResponse => Impulse.Response;
        /// <summary>Filter polarity, true if positive.</summary>
        public bool Polarity => Impulse.Polarity;
        /// <summary>Response delay in seconds.</summary>
        public float Delay => Impulse.Delay / (float)sampleRate;

        /// <summary>Frequency response of the filter.</summary>
        Complex[] FrequencyResponse {
            get {
                if (frequencyResponse != null)
                    return frequencyResponse;
                float[] reference = Measurements.ExponentialSweep(1, sampleRate / 2, 65536, sampleRate), response = (float[])reference.Clone();
                filter.Process(response);
                return frequencyResponse = Measurements.GetFrequencyResponse(reference, response);
            }
        }
        Complex[] frequencyResponse;

        /// <summary>Absolute of <see cref="FrequencyResponse"/> up to half the sample rate.</summary>
        float[] Spectrum {
            get {
                if (spectrum == null)
                    spectrum = Measurements.GetSpectrum(FrequencyResponse);
                return spectrum;
            }
        }
        float[] spectrum;

        /// <summary>Impulse response processor.</summary>
        VerboseImpulseResponse Impulse => impulse ?? (impulse = new VerboseImpulseResponse(FrequencyResponse));

        /// <summary>Cached <see cref="Gain"/>.</summary>
        float gain = float.NaN;
        /// <summary>Cached <see cref="Impulse"/>.</summary>
        VerboseImpulseResponse impulse;

        /// <summary>Filter to measure.</summary>
        readonly Filter filter;
        /// <summary>Sample rate used for measurements and in <see cref="filter"/> if it's sample rate-dependent.</summary>
        readonly int sampleRate;

        /// <summary>Copy a filter for measurements.</summary>
        /// <param name="filter">Filter to measure</param>
        /// <param name="sampleRate">Sample rate used for measurements and in <paramref name="filter"/> if it's sample rate-dependent</param>
        public FilterAnalyzer(Filter filter, int sampleRate) {
            this.filter = filter;
            this.sampleRate = sampleRate;
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