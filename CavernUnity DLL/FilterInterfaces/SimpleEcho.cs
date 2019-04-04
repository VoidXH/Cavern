using UnityEngine;

using Cavern.Filters;

namespace Cavern.FilterInterfaces {
    /// <summary>Apply an <see cref="Echo"/> filter on the source the component is applied on.</summary>
    [AddComponentMenu("Audio/Filters/Simple Echo")]
    [RequireComponent(typeof(AudioSource3D))]
    public class SimpleEcho : MonoBehaviour {
        /// <summary>Effect strength.</summary>
        [Tooltip("Effect strength.")]
        [Range(0, 1)] public float Strength = .25f;
        /// <summary>Delay between echoes in seconds.</summary>
        [Tooltip("Delay between echoes in seconds.")]
        [Range(0.01f, 1f)] public float Delay = .1f;

        /// <summary>The attached audio source.</summary>
        AudioSource3D Source;
        /// <summary>The attached echo filter.</summary>
        Echo Filter;

        void Start() {
            Source = GetComponent<AudioSource3D>();
            Filter = new Echo(Strength, Delay);
            Source.AddFilter(Filter);
        }

        void OnDestroy() => Source.RemoveFilter(Filter);

        void Update() {
            int TargetDelay = (int)(Delay * AudioListener3D.Current.SampleRate);
            if (Filter.DelaySamples != TargetDelay)
                Filter.DelaySamples = TargetDelay;
            Filter.Strength = Strength;
        }
    }
}