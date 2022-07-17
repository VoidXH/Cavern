using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.FilterInterfaces {
    /// <summary>
    /// Apply a <see cref="Filters.Comb"/> filter on the source this component is applied on.
    /// </summary>
    [AddComponentMenu("Audio/Filters/Comb")]
    [RequireComponent(typeof(AudioSource3D))]
    public class Comb : MonoBehaviour {
        /// <summary>
        /// First minimum point.
        /// </summary>
        [Tooltip("First minimum point.")]
        [Range(20, 3000)] public float frequency = 1000;

        /// <summary>
        /// Wet mix multiplier.
        /// </summary>
        [Tooltip("Wet mix multiplier")]
        [Range(0, 3)] public float alpha;

        /// <summary>
        /// The attached audio source.
        /// </summary>
        AudioSource3D source;

        /// <summary>
        /// The attached delay filter.
        /// </summary>
        Filters.Comb filter;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() {
            source = GetComponent<AudioSource3D>();
            filter = new Filters.Comb(AudioListener3D.Current.SampleRate, frequency, alpha);
            source.AddFilter(filter);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() => source.RemoveFilter(filter);

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            filter.Frequency = frequency;
            filter.Alpha = alpha;
        }
    }
}