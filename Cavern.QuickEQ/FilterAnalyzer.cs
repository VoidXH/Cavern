using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Cavern.Filters;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Measures properties of a filter, like frequency/impulse response, gain, or delay.</summary>
    public sealed class FilterAnalyzer : IDisposable {
        /// <summary>Used FFT size for most measurements.</summary>
        public int Resolution {
            get => resolution;
            set {
                if (resolution != value) {
                    if (cache != null)
                        Dispose();
                    cache = null;
                    resolution = value;
                }
                gain = float.NaN;
                impulseReference = null;
                frequencyResponse = null;
                spectrum = null;
                impulse = null;
            }
        }
        int resolution = 65536;

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
                    return cache = new FFTCache(Resolution);
                return cache;
            }
        }
        FFTCache cache;

        /// <summary>Swept sine used for frequency and impulse response measurements.</summary>
        float[] ImpulseReference {
            get {
                if (impulseReference != null)
                    return impulseReference;
                impulseReference = new float[Resolution];
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
                    float[] response = ImpulseReference.FastClone();
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
            if (SampleRate != sampleRate)
                SampleRate = sampleRate;
        }

        /// <summary>Get the frequency response of the filter.</summary>
        public Complex[] GetFrequencyResponse() => FrequencyResponse.FastClone();

        /// <summary>Get the frequency response of the filter.</summary>
        public ReadOnlyCollection<Complex> GetFrequencyResponseReadonly() => Array.AsReadOnly(FrequencyResponse);

        /// <summary>Get the absolute of <see cref="FrequencyResponse"/> up to half the sample rate.</summary>
        public float[] GetSpectrum() => Spectrum.FastClone();

        /// <summary>Get the absolute of <see cref="FrequencyResponse"/> up to half the sample rate.</summary>
        public ReadOnlyCollection<float> GetSpectrumReadonly() => Array.AsReadOnly(Spectrum);

        /// <summary>Render an approximate <see cref="Equalizer"/> by the analyzed filter's frequency response.</summary>
        /// <param name="startFreq">Start of the rendered range</param>
        /// <param name="endFreq">End of the rendered range</param>
        /// <param name="resolution">Band diversity in octaves</param>
        /// <param name="oversampling">Detail increase factor</param>
        public Equalizer ToEqualizer(double startFreq, double endFreq, double resolution = 1 / 3f, int oversampling = 1) {
            float[] graph = GraphUtils.ConvertToGraph(FrequencyResponse, startFreq, endFreq, SampleRate, SampleRate * oversampling);
            List<Band> bands = new List<Band>();
            double startPow = Math.Log10(startFreq), powRange = (Math.Log10(endFreq) - startPow) / graph.Length,
                octaveRange = Math.Log(endFreq, 2) - Math.Log(startFreq, 2);
            int windowSize = (int)(graph.Length / (octaveRange / resolution + 1));
            for (int pos = graph.Length - 1; pos >= 0; pos -= windowSize)
                bands.Add(new Band(Math.Pow(10, startPow + powRange * pos), 20 * Math.Log10(graph[pos])));
            bands.Reverse();
            return new Equalizer(bands);
        }

        /// <summary>Free the resources used by this analyzer.</summary>
        public void Dispose() {
            if (CavernAmp.Available && cache != null)
                cache.Dispose();
        }
    }
}