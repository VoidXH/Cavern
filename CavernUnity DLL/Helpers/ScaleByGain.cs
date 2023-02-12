using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.Helpers {
    /// <summary>
    /// Scale an object by an <see cref="AudioSource3D"/>'s current gain.
    /// </summary>
    [AddComponentMenu("Audio/Helpers/Gain-based Object Scaler")]
    public class ScaleByGain : MonoBehaviour {
        /// <summary>
        /// Target source.
        /// </summary>
        [Tooltip("Target source.")]
        public AudioSource3D source;

        /// <summary>
        /// Object size change smoothness.
        /// </summary>
        [Tooltip("Object size change smoothness.")]
        [Range(0, .95f)] public float smoothing = .8f;

        /// <summary>
        /// Object scale at minimum gain.
        /// </summary>
        [Tooltip("Object scale at minimum gain.")]
        [Range(.1f, 10)] public float minSize = .1f;

        /// <summary>
        /// Object scale at maximum gain.
        /// </summary>
        [Tooltip("Object scale at maximum gain.")]
        [Range(.1f, 10)] public float maxSize = 1.25f;

        /// <summary>
        /// Signal level at minimum size.
        /// </summary>
        [Tooltip("Signal level at minimum size.")]
        [Range(-300, 0)] public float dynamicRange = -96;

        /// <summary>
        /// Actual scaling value.
        /// </summary>
        float scale;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            MultichannelWaveform samples = source.cavernSource.Rendered;
            if (samples != null) {
                float peakSize = float.NegativeInfinity;
                for (int channel = 0; channel < samples.Channels; channel++) {
                    float channelSize = 20 * (float)Math.Log10(WaveformUtils.GetPeak(samples[channel]));
                    if (channelSize < -600) {
                        channelSize = -600;
                    }
                    if (peakSize < channelSize) {
                        peakSize = channelSize;
                    }
                }
                float size = Mathf.Clamp(peakSize / -dynamicRange + 1, 0, 1);
                scale = QMath.Lerp(scale, (maxSize - minSize) * size + minSize, 1 - smoothing);
                transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }
}