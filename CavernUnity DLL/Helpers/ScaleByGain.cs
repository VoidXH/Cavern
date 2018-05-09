using UnityEngine;

namespace Cavern.Helpers {
    /// <summary>Scale an object by an <see cref="AudioSource3D"/>'s current gain.</summary>
    [AddComponentMenu("Audio/Helpers/Gain-based Object Scaler")]
    public class ScaleByGain : MonoBehaviour {
        /// <summary>Target source.</summary>
        [Tooltip("Target source.")]
        public AudioSource3D Source;

        /// <summary>Object size change smoothness.</summary>
        [Tooltip("Object size change smoothness.")]
        [Range(0, .95f)] public float Smoothing = .8f;
        /// <summary>Object scale at minimum gain.</summary>
        [Tooltip("Object scale at minimum gain.")]
        [Range(.1f, 10)] public float MinSize = .1f;
        /// <summary>Object scale at maximum gain.</summary>
        [Tooltip("Object scale at maximum gain.")]
        [Range(.1f, 10)] public float MaxSize = 1.25f;

        /// <summary>Samples to check for gain.</summary>
        [Tooltip("Samples to check for gain.")]
        public int SampleCount = 512;

        /// <summary>Signal level at minimum size.</summary>
        [Tooltip("Signal level at minimum size.")]
        [Range(-300, 0)] public float DynamicRange = -96;

        /// <summary>Actual scaling value.</summary>
        float Scale = 0;

        void Update() {
            float[] Samples = new float[SampleCount];
            Source.Clip.GetData(Samples, Source.timeSamples);
            float Size = Mathf.Clamp(CavernUtilities.GetPeak(Samples, SampleCount) / -DynamicRange + 1, 0, 1);
            Scale = CavernUtilities.FastLerp(Scale, (MaxSize - MinSize) * Size + MinSize, 1 - Smoothing);
            transform.localScale = new Vector3(Scale, Scale, Scale);
        }
    }
}