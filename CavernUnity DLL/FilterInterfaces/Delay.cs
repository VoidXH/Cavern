using UnityEngine;

namespace Cavern.FilterInterfaces {
    /// <summary>Apply a <see cref="Filters.Delay"/> filter on the source this component is applied on.</summary>
    [AddComponentMenu("Audio/Filters/Delay")]
    [RequireComponent(typeof(AudioSource3D))]
    public class Delay : MonoBehaviour {
        /// <summary>Delay in seconds.</summary>
        [Tooltip("Delay in seconds.")]
        [Range(0, 3)] public float DelayTime = .25f;

        /// <summary>The attached audio source.</summary>
        AudioSource3D source;
        /// <summary>The attached delay filter.</summary>
        Filters.Delay filter;

        void OnEnable() {
            source = GetComponent<AudioSource3D>();
            filter = new Filters.Delay(DelayTime, AudioListener3D.Current.SampleRate);
            source.AddFilter(filter);
        }

        void OnDisable() => source.RemoveFilter(filter);

        void Update() => filter.DelaySamples = (int)(DelayTime * AudioListener3D.Current.SampleRate);
    }
}