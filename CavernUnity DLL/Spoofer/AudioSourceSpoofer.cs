using System;
using UnityEngine;

namespace Cavern.Spoofer {
    /// <summary>Converts a regular <see cref="AudioSource"/> to Cavern's <see cref="AudioSource3D"/>.</summary>
    [AddComponentMenu("Audio/Spoofer/Audio Source")]
    public sealed class AudioSourceSpoofer : MonoBehaviour {
        /// <summary>Source to spoof.</summary>
        public AudioSource Source;

        /// <summary>Use Unity's audio engine for clips that are not transferrable to Cavern (transferred from <see cref="AutoSpoofer"/>).</summary>
        internal bool Duality = true;

        AudioSource3D Target;

        void LateUpdate() {
            if (Source) {
                if (!Target)
                    Target = Source.gameObject.AddComponent<AudioSource3D>();
                Target.enabled = Source.enabled;
                Target.DopplerLevel = Source.dopplerLevel;
                Target.IsPlaying = Source.isPlaying;
                Target.Loop = Source.loop;
                Target.Mute = Source.mute;
                Target.Pitch = Source.pitch;
                Target.SpatialBlend = Source.spatialBlend;
                Target.StereoPan = Source.panStereo;
                if (Target.Clip = Source.clip) {
                    bool Decompressed = Target.Clip.loadType == AudioClipLoadType.DecompressOnLoad;
                    if (!Duality)
                        Target.Volume = Source.volume;
                    else if (Decompressed && Source.volume != 0.00001f) {
                        Target.Volume = Source.volume;
                        Source.volume = 0.00001f; // Not zero, but unlikely to be heard.
                    }
                    if (Target.Clip && Math.Abs(Target.timeSamples - Source.timeSamples) > Target.Clip.frequency / 60)
                        Target.timeSamples = Source.timeSamples;
                    if (!Decompressed)
                        Target.Clip = null;
                }
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
            } else {
                if (Target)
                    Destroy(Target);
                Destroy(this);
            }
        }
    }
}