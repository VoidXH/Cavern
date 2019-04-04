using UnityEngine;

using Cavern.Filters;

namespace Cavern.FilterInterfaces {
    /// <summary>The further the source from the listener, the deeper this effect will make its sound.</summary>
    [AddComponentMenu("Audio/Filters/Distance-based lowpass")]
    [RequireComponent(typeof(AudioSource3D))]
    public class DistanceBasedLowpass : MonoBehaviour {
        /// <summary>Effect strength multiplier.</summary>
        [Tooltip("Effect strength multiplier.")]
        [Range(0, 1)] public float Strength = .1f;

        /// <summary>The attached audio source.</summary>
        AudioSource3D Source;
        /// <summary>The attached lowpass filter.</summary>
        Lowpass Filter;

        void Start() {
            Source = GetComponent<AudioSource3D>();
            Filter = new Lowpass(120);
            Source.AddFilter(Filter);
        }

        void OnDestroy() => Source.RemoveFilter(Filter);

        void Update() {
            if (!float.IsNaN(Source.Distance)) {
                float DistanceScale = Source.Distance * Strength;
                if (DistanceScale > 1)
                    Filter.Reset(120 + 20000 / DistanceScale);
            }
        }
    }
}