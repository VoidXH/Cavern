using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Helpers {
    /// <summary>
    /// Handles and synchronizes an <see cref="AudioSource3D"/> with an input device.
    /// </summary>
    [AddComponentMenu("Audio/Helpers/External Source")]
    public class ExternalSource : MonoBehaviour {
        /// <summary>
        /// Actual latency.
        /// </summary>
        public float Latency { get; private set; }

        /// <summary>
        /// Target source.
        /// </summary>
        [Tooltip("Target source.")]
        public AudioSource3D source;

        /// <summary>
        /// Audio input device.
        /// </summary>
        [Tooltip("Audio input device.")]
        public string sourceName;

        /// <summary>
        /// Maximum allowed latency.
        /// </summary>
        [Tooltip("Maximum allowed latency.")]
        [Range(.02f, .5f)] public float maxLatency = .05f;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() {
            source.Loop = true;
            Microphone.GetDeviceCaps(sourceName, out _, out int maxFreq);
            source.clip = Microphone.Start(sourceName, true, 1, maxFreq);
            source.clip.name = sourceName;
            source.Play();
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (!source) {
                return;
            }
            // Latency fix
            int micPos = Microphone.GetPosition(source.clip.name);
            Latency = (micPos - source.timeSamples + source.clip.samples) % source.clip.samples / (float)source.clip.samples;
            if (Latency > maxLatency) {
                source.timeSamples = micPos - AudioSettings.GetConfiguration().dspBufferSize;
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() => Destroy(source.clip);
    }
}