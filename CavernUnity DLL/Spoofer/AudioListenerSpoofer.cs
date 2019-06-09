using UnityEngine;

namespace Cavern.Spoofer {
    /// <summary>Converts a regular <see cref="AudioListener"/> to Cavern's <see cref="AudioListener3D"/>.</summary>
    [AddComponentMenu("Audio/Spoofer/Audio Listener")]
    public sealed class AudioListenerSpoofer : MonoBehaviour {
        /// <summary>Listener to spoof.</summary>
        [Tooltip("Listener to spoof.")]
        public AudioListener Source;

        /// <summary>Use Unity's audio engine for clips that are not transferrable to Cavern (transferred from <see cref="AutoSpoofer"/>).</summary>
        internal bool duality = true;

        AudioListener3D target;

        void LateUpdate() {
            if (Source) {
                if (!target) {
                    if (AudioListener3D.Current)
                        target = AudioListener3D.Current;
                    else
                        target = Source.gameObject.AddComponent<AudioListener3D>();
                }
                target.enabled = Source.enabled;
                target.Paused = AudioListener.pause;
                if (duality)
                    target.volume = AudioListener.volume;
                else if (AudioListener.volume != AudioSourceSpoofer.Mute) {
                    target.Volume = AudioListener.volume;
                    AudioListener.volume = AudioSourceSpoofer.Mute;
                }
            } else {
                if (target)
                    Destroy(target);
                Destroy(this);
            }
        }
    }
}