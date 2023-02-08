using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Filters;

namespace Cavern.FilterInterfaces {
    /// <summary>
    /// Apply an <see cref="Echo"/> filter on the source this component is applied on.
    /// </summary>
    [AddComponentMenu("Audio/Filters/Simple Echo")]
    [RequireComponent(typeof(AudioSource3D))]
    public class SimpleEcho : MonoBehaviour {
        /// <summary>
        /// Effect strength.
        /// </summary>
        [Tooltip("Effect strength.")]
        [Range(0, 1)] public float Strength = .25f;

        /// <summary>
        /// Delay between echoes in seconds.
        /// </summary>
        [Tooltip("Delay between echoes in seconds.")]
        [Range(0.01f, 1f)] public float Delay = .1f;

        /// <summary>
        /// The attached audio source.
        /// </summary>
        AudioSource3D source;

        /// <summary>
        /// The attached echo filter.
        /// </summary>
        Echo filter;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() {
            source = GetComponent<AudioSource3D>();
            filter = new Echo(AudioListener3D.Current.SampleRate, Strength, Delay);
            source.AddFilter(filter);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() => source.RemoveFilter(filter);

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            int targetDelay = (int)(Delay * AudioListener3D.Current.SampleRate);
            if (filter.DelaySamples != targetDelay) {
                filter.DelaySamples = targetDelay;
            }
            filter.Strength = Strength;
        }
    }
}