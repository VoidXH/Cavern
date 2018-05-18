using System;
using UnityEngine;

namespace Cavern.Spoofer {
    /// <summary>Converts a regular <see cref="AudioSource"/> to Cavern's <see cref="AudioSource3D"/>.</summary>
    [AddComponentMenu("Audio/Spoofer/Audio Source")]
    public sealed class AudioSourceSpoofer : MonoBehaviour {
        /// <summary>Source to spoof.</summary>
        public AudioSource Source;

        AudioSource3D Target;

        void LateUpdate() {
            if (Source) {
                if (!Target)
                    Target = Source.gameObject.AddComponent<AudioSource3D>();
                Target.enabled = Source.enabled;
                Target.Clip = Source.clip;
                Target.DopplerLevel = Source.dopplerLevel;
                Target.IsPlaying = Source.isPlaying;
                Target.Loop = Source.loop;
                Target.Mute = Source.mute;
                Target.Pitch = Source.pitch;
                Target.SpatialBlend = Source.spatialBlend;
                Target.StereoPan = Source.panStereo;
                Target.Volume = Source.volume;
                switch (Source.rolloffMode) {
                    case AudioRolloffMode.Linear:
                        Target.VolumeRolloff = Rolloffs.Linear;
                        break;
                    case AudioRolloffMode.Logarithmic:
                        Target.VolumeRolloff = Rolloffs.Logarithmic;
                        break;
                    default:
                        Target.VolumeRolloff = Rolloffs.Disabled;
                        break;
                }
                if (Target.Clip && Math.Abs(Target.timeSamples - Source.timeSamples) > Target.Clip.frequency / 60)
                    Target.timeSamples = Source.timeSamples;
            } else {
                if (Target)
                    Destroy(Target);
                Destroy(this);
            }
        }
    }
}