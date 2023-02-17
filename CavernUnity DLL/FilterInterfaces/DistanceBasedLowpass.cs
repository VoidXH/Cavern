using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Filters;

namespace Cavern.FilterInterfaces {
    /// <summary>
    /// The further the source from the listener, the deeper this effect will make its sound.
    /// </summary>
    [AddComponentMenu("Audio/Filters/Distance-based Lowpass")]
    [RequireComponent(typeof(AudioSource3D))]
    public class DistanceBasedLowpass : MonoBehaviour {
        /// <summary>
        /// Effect strength multiplier.
        /// </summary>
        [Tooltip("Effect strength multiplier.")]
        [Range(0, 1)] public float Strength = .1f;

        /// <summary>
        /// The attached audio source.
        /// </summary>
        AudioSource3D source;

        /// <summary>
        /// The attached lowpass filter.
        /// </summary>
        Lowpass filter;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() {
            source = GetComponent<AudioSource3D>();
            filter = new Lowpass(AudioListener3D.Current.SampleRate, 120);
            Update();
            source.AddFilter(filter);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() => source.RemoveFilter(filter);

        void Update() {
            float distance = (source.cavernSource.Position - AudioListener3D.cavernListener.Position).Length(),
                distanceScale = distance * Strength;
            if (distanceScale > 1) {
                filter.Reset(120 + 20000 / distanceScale);
            }
        }
    }
}