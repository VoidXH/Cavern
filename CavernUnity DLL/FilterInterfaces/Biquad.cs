using UnityEngine;

using Cavern.Filters;
using Cavern.Filters.Utilities;

namespace Cavern.FilterInterfaces {
    /// <summary>Apply a <see cref="BiquadFilter"/> on the source this component is applied on.</summary>
    [AddComponentMenu("Audio/Filters/Biquad Filter")]
    [RequireComponent(typeof(AudioSource3D))]
    public class Biquad : MonoBehaviour {
        /// <summary>Possible biquad filter types.</summary>
        public enum FilterTypes {
            /// <summary>Lowpass filter.</summary>
            Lowpass,
            /// <summary>Highpass filter.</summary>
            Highpass,
            /// <summary>Bandpass filter.</summary>
            Bandpass,
            /// <summary>Notch filter.</summary>
            Notch,
            /// <summary>Allpass filter.</summary>
            Allpass,
            /// <summary>Peaking filter.</summary>
            PeakingEQ,
            /// <summary>Low shelf filter.</summary>
            LowShelf,
            /// <summary>High shelf filter.</summary>
            HighShelf
        };
        /// <summary>Applied type of biquad filter.</summary>
        [Tooltip("Applied type of biquad filter.")]
        public FilterTypes FilterType;
        /// <summary>Center frequency (-3 dB point) of the filter.</summary>
        [Tooltip("Center frequency (-3 dB point) of the filter.")]
        [Range(20, 20000)] public double CenterFreq = 1000;
        /// <summary>Q-factor of the filter.</summary>
        [Tooltip("Q-factor of the filter.")]
        [Range(1/3f, 100/3f)] public double Q = QFactor.reference;
        /// <summary>Gain of the filter in decibels.</summary>
        [Tooltip("Gain of the filter in decibels.")]
        [Range(-10, 10)] public double Gain = 0;

        /// <summary>The attached audio source.</summary>
        AudioSource3D source;
        /// <summary>The attached selected filter.</summary>
        BiquadFilter filter;
        /// <summary>Last set type of filter.</summary>
        FilterTypes lastFilter;

        void RecreateFilter() {
            if (filter != null)
                source.RemoveFilter(filter);
            source.AddFilter(filter = (lastFilter = FilterType) switch {
                FilterTypes.Lowpass => new Lowpass(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain),
                FilterTypes.Highpass => new Highpass(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain),
                FilterTypes.Bandpass => new Bandpass(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain),
                FilterTypes.Notch => new Notch(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain),
                FilterTypes.Allpass => new Allpass(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain),
                FilterTypes.PeakingEQ => new PeakingEQ(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain),
                FilterTypes.LowShelf => new LowShelf(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain),
                FilterTypes.HighShelf => new HighShelf(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain),
                _ => throw new FilterNotExistsException(),
            });
        }

        void OnEnable() {
            source = GetComponent<AudioSource3D>();
            RecreateFilter();
        }

        void OnDisable() {
            source.RemoveFilter(filter);
            filter = null;
        }

        void Update() {
            if (lastFilter != FilterType)
                RecreateFilter();
            if (filter.CenterFreq != CenterFreq || filter.Q != Q || filter.Gain != Gain)
                filter.Reset(CenterFreq, Q, Gain);
        }
    }
}