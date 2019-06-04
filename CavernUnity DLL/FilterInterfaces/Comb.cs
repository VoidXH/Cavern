using UnityEngine;

namespace Cavern.FilterInterfaces {
    /// <summary>Apply a <see cref="Filters.Comb"/> filter on the source this component is applied on.</summary>
    [AddComponentMenu("Audio/Filters/Comb")]
    [RequireComponent(typeof(AudioSource3D))]
    public class Comb : MonoBehaviour {
        /// <summary>First minimum point.</summary>
        [Tooltip("First minimum point.")]
        [Range(20, 3000)] public float Frequency = 1000;
        /// <summary>Wet mix multiplier.</summary>
        [Tooltip("Wet mix multiplier")]
        [Range(0, 3)] public float Alpha;

        /// <summary>The attached audio source.</summary>
        AudioSource3D Source;
        /// <summary>The attached delay filter.</summary>
        Filters.Comb Filter;

        void OnEnable() {
            Source = GetComponent<AudioSource3D>();
            Filter = new Filters.Comb(AudioListener3D.Current.SampleRate, Frequency, Alpha);
            Source.AddFilter(Filter);
        }

        void OnDisable() => Source.RemoveFilter(Filter);

        void Update() {
            Filter.Frequency = Frequency;
            Filter.Alpha = Alpha;
        }
    }
}