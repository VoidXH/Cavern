using UnityEngine;

namespace Cavern.Spoofer {
    /// <summary>Converts a regular <see cref="AudioListener"/> to Cavern's <see cref="AudioListener3D"/>.</summary>
    [AddComponentMenu("Audio/Spoofer/Audio Listener")]
    public sealed class AudioListenerSpoofer : MonoBehaviour {
        /// <summary>Listener to spoof.</summary>
        [Tooltip("Listener to spoof.")]
        public AudioListener Source;

        /// <summary>Use Unity's audio engine for clips that are not transferrable to Cavern (transferred from <see cref="AutoSpoofer"/>).</summary>
        internal bool Duality = true;

        AudioListener3D Target;

        void LateUpdate() {
            if (Source) {
                if (!Target) {
                    if (AudioListener3D.Current)
                        Target = AudioListener3D.Current;
                    else
                        Target = Source.gameObject.AddComponent<AudioListener3D>();
                }
                Target.enabled = Source.enabled;
                Target.Paused = AudioListener.pause;
                if (Duality)
                    Target.volume = AudioListener.volume;
                else if (AudioListener.volume != 0.00001f) {
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