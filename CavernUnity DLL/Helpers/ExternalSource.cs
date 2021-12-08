using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Helpers {
    /// <summary>
    /// Handles and synchronizes an <see cref="AudioSource3D"/> with an input device.
    /// </summary>
    [AddComponentMenu("Audio/Helpers/External Source")]
    public class ExternalSource : MonoBehaviour {
        /// <summary>
        /// Target source.
        /// </summary>
        [Tooltip("Target source.")]
        public AudioSource3D Source;

        /// <summary>
        /// Audio input device.
        /// </summary>
        [Tooltip("Audio input device.")]
        public string SourceName;

        /// <summary>
        /// Maximum allowed latency.
        /// </summary>
        [Tooltip("Maximum allowed latency.")]
        [Range(.02f, .5f)] public float MaxLatency = .05f;

        /// <summary>
        /// Actual latency.
        /// </summary>
        public float Latency { get; private set; }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() {
            Source.Loop = true;
            Microphone.GetDeviceCaps(SourceName, out _, out int maxFreq);
            Source.clip = Microphone.Start(SourceName, true, 1, maxFreq);
            Source.clip.name = SourceName;
            Source.Play();
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (!Source)
                return;
            // Latency fix
            int micPos = Microphone.GetPosition(Source.clip.name);
            Latency = (micPos - Source.timeSamples + Source.clip.samples) % Source.clip.samples / (float)Source.clip.samples;
            if (Latency > MaxLatency)
                Source.timeSamples = micPos - AudioSettings.GetConfiguration().dspBufferSize;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() => Destroy(Source.clip);
    }
}