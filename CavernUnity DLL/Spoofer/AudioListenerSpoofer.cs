using UnityEngine;

namespace Cavern.Spoofer {
    /// <summary>Converts a regular <see cref="AudioListener"/> to Cavern's <see cref="AudioListener3D"/>.</summary>
    [AddComponentMenu("Audio/Spoofer/Audio Listener"), RequireComponent(typeof(AudioListener))]
    public sealed class AudioListenerSpoofer : MonoBehaviour {
        /// <summary>Listener to spoof.</summary>
        public AudioListener Source;

        AudioListener3D Target;

        void LateUpdate() {
            if (Source) {
                if (!Target)
                    Target = Source.gameObject.AddComponent<AudioListener3D>();
                Target.enabled = Source.enabled;
                Target.Paused = AudioListener.pause;
                if (AudioListener.volume != 0.00001f) {
                    Target.Volume = AudioListener.volume;
                    AudioListener.volume = 0.00001f; // Not zero, but unlikely to be heard.
                }
            } else {
                if (Target)
                    Destroy(Target);
                Destroy(this);
            }
        }
    }
}