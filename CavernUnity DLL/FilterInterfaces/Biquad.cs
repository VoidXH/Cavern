using UnityEngine;

using Cavern.Filters;

namespace Cavern.FilterInterfaces {
    /// <summary>Apply a <see cref="BiquadFilter"/> on the source the component is applied on.</summary>
    [AddComponentMenu("Audio/Filters/Biquad Filter")]
    [RequireComponent(typeof(AudioSource3D))]
    public class Biquad : MonoBehaviour {
        /// <summary>Possible biquad filter types.</summary>
        public enum FilterTypes {
            /// <summary>Low-pass filter.</summary>
            Lowpass,
            /// <summary>High-pass filter.</summary>
            Highpass
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

        /// <summary>The attached audio source.</summary>
        AudioSource3D Source;
        /// <summary>The attached selected filter.</summary>
        BiquadFilter Filter;

        void RecreateFilter() {
            if (Filter != null)
                Source.RemoveFilter(Filter);
            switch (FilterType) {
                case FilterTypes.Lowpass:
                    Filter = new Lowpass(CenterFreq, Q);
                    break;
                case FilterTypes.Highpass:
                    Filter = new Highpass(CenterFreq, Q);
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
            switch (FilterType) {
                case FilterTypes.Lowpass:
                    if (!(Filter is Lowpass))
                        RecreateFilter();
                    break;
                case FilterTypes.Highpass:
                    if (!(Filter is Highpass))
                        RecreateFilter();
                    break;
            }
            if (Filter.CenterFreq != CenterFreq || Filter.Q != Q)
                Filter.Reset(CenterFreq, Q);
        }
    }
}
