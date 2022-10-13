using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Spoofer {
    /// <summary>
    /// Converts a regular <see cref="AudioListener"/> to Cavern's <see cref="AudioListener3D"/>.
    /// </summary>
    [AddComponentMenu("Audio/Spoofer/Audio Listener")]
    public sealed class AudioListenerSpoofer : MonoBehaviour {
        /// <summary>
        /// Listener to spoof.
        /// </summary>
        [Tooltip("Listener to spoof.")]
        public AudioListener Source;

        /// <summary>
        /// Use Unity's audio engine for clips that are not transferrable to Cavern (transferred from <see cref="AutoSpoofer"/>).
        /// </summary>
        internal bool duality = true;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() => AudioListener3D.Current.DisableUnityAudio = !duality;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void LateUpdate() {
            if (Source) {
                AudioListener3D target = AudioListener3D.Current;
                if (!target) {
                    target = Source.gameObject.AddComponent<AudioListener3D>();
                }
                target.enabled = Source.enabled;
                target.Paused = AudioListener.pause;
                if (duality) {
                    AudioListener3D.volume = AudioListener.volume;
                } else if (AudioListener.volume != AudioSourceSpoofer.Mute) {
                    AudioListener3D.volume = AudioListener.volume;
                    AudioListener.volume = AudioSourceSpoofer.Mute;
                }
            } else {
                if (AudioListener3D.Current) {
                    Destroy(AudioListener3D.Current);
                }
                Destroy(this);
            }
        }
    }
}