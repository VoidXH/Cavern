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
        AudioSource3D Source;
        /// <summary>The attached delay filter.</summary>
        Filters.Delay Filter;

        void OnEnable() {
            Source = GetComponent<AudioSource3D>();
            Filter = new Filters.Delay(DelayTime, AudioListener3D.Current.SampleRate);
            Source.AddFilter(Filter);
        }

        void OnDisable() => Source.RemoveFilter(Filter);

        void Update() {
            int TargetDelay = (int)(DelayTime * AudioListener3D.Current.SampleRate);
            if (Filter.DelaySamples != TargetDelay)
                Filter.DelaySamples = TargetDelay;
        }
    }
}