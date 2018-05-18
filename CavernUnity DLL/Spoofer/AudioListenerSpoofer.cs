using UnityEngine;

namespace Cavern.Spoofer {
    /// <summary>Converts a regular <see cref="AudioListener"/> to Cavern's <see cref="AudioListener3D"/>.</summary>
    [AddComponentMenu("Audio/Spoofer/Audio Listener"), RequireComponent(typeof(AudioListener))]
    public sealed class AudioListenerSpoofer : MonoBehaviour {
        AudioListener Source;
        AudioListener3D Target;

        void Update() {
            if (Source) {
                Target.enabled = Source.enabled;
                Target.Paused = AudioListener.pause;
                if (AudioListener.volume != 0) {
                    Target.Volume = AudioListener.volume;
                    AudioListener.volume = 0;
                }
            } else if (Source = GetComponent<AudioListener>())
                Target = gameObject.AddComponent<AudioListener3D>();
            else
                Destroy(Target);
        }
    }
}