using UnityEngine;

using Cavern.Filters;

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
        [Range(20, 20000)] public float CenterFreq = 1000;
        /// <summary>Q-factor of the filter.</summary>
        [Tooltip("Q-factor of the filter.")]
        [Range(1/3f, 100/3f)] public float Q = .7071067811865475f;
        /// <summary>Gain of the filter in decibels.</summary>
        [Tooltip("Gain of the filter in decibels.")]
        [Range(-10, 10)] public float Gain = 0;

        /// <summary>The attached audio source.</summary>
        AudioSource3D Source;
        /// <summary>The attached selected filter.</summary>
        BiquadFilter Filter;
        /// <summary>Last set type of filter.</summary>
        FilterTypes LastFilter;

        void RecreateFilter() {
            if (Filter != null)
                Source.RemoveFilter(Filter);
            switch (LastFilter = FilterType) {
                case FilterTypes.Lowpass:
                    Filter = new Lowpass(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain);
                    break;
                case FilterTypes.Highpass:
                    Filter = new Highpass(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain);
                    break;
                case FilterTypes.Bandpass:
                    Filter = new Bandpass(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain);
                    break;
                case FilterTypes.Notch:
                    Filter = new Notch(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain);
                    break;
                case FilterTypes.Allpass:
                    Filter = new Allpass(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain);
                    break;
                case FilterTypes.PeakingEQ:
                    Filter = new PeakingEQ(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain);
                    break;
                case FilterTypes.LowShelf:
                    Filter = new LowShelf(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain);
                    break;
                case FilterTypes.HighShelf:
                    Filter = new HighShelf(AudioListener3D.Current.SampleRate, CenterFreq, Q, Gain);
                    break;
            }
            Source.AddFilter(Filter);
        }

        void OnEnable() {
            Source = GetComponent<AudioSource3D>();
            RecreateFilter();
        }

        void OnDisable() {
            Source.RemoveFilter(Filter);
            Filter = null;
        }

        void Update() {
            if (LastFilter != FilterType)
                RecreateFilter();
            if (Filter.CenterFreq != CenterFreq || Filter.Q != Q || Filter.Gain != Gain)
                Filter.Reset(CenterFreq, Q, Gain);
        }
    }
}
