using System;
using UnityEngine;

using Cavern.Utilities;

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

        /// <summary>Signal level at minimum size.</summary>
        [Tooltip("Signal level at minimum size.")]
        [Range(-300, 0)] public float DynamicRange = -96;

        /// <summary>Actual scaling value.</summary>
        float scale;

        void Update() {
            float[][] samples = Source.cavernSource.Rendered;
            if (samples != null) {
                float peakSize = float.NegativeInfinity;
                for (int channel = 0; channel < samples.Length; ++channel) {
                    float channelSize = 20 * (float)Math.Log10(WaveformUtils.GetPeak(samples[channel]));
                    if (channelSize < -600)
                        channelSize = -600;
                    if (peakSize < channelSize)
                        peakSize = channelSize;
                }
                float size = Mathf.Clamp(peakSize / -DynamicRange + 1, 0, 1);
                scale = QMath.Lerp(scale, (MaxSize - MinSize) * size + MinSize, 1 - Smoothing);
                transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }
}