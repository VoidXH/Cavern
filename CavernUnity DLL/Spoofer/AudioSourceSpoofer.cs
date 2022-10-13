using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Spoofer {
    /// <summary>
    /// Converts a regular <see cref="AudioSource"/> to Cavern's <see cref="AudioSource3D"/>.
    /// </summary>
    [AddComponentMenu("Audio/Spoofer/Audio Source")]
    public sealed class AudioSourceSpoofer : MonoBehaviour {
        /// <summary>
        /// -60 dB signal level. Not zero, but unlikely to be heard. In case a newly set 0 should be detected.
        /// </summary>
        internal const float Mute = 0.000001f;

        /// <summary>
        /// Source to spoof.
        /// </summary>
        [Tooltip("Source to spoof.")]
        public AudioSource Source;

        /// <summary>
        /// Use Unity's audio engine for clips that are not transferrable to Cavern (transferred from <see cref="AutoSpoofer"/>).
        /// </summary>
        internal bool duality = true;

        AudioSource3D target;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void LateUpdate() {
            if (Source) {
                if (!target) {
                    target = Source.gameObject.AddComponent<AudioSource3D>();
                }
                target.enabled = Source.enabled;
                target.DopplerLevel = Source.dopplerLevel;
                target.IsPlaying = Source.isPlaying;
                target.Loop = Source.loop;
                target.Mute = Source.mute;
                target.Pitch = Source.pitch;
                target.SpatialBlend = Source.spatialBlend;
                target.StereoPan = Source.panStereo;
                if (target.Clip = Source.clip) {
                    bool decompressed = Source.clip.loadType == AudioClipLoadType.DecompressOnLoad;
                    if (!duality) {
                        target.Volume = Source.volume;
                    } else if (decompressed && Source.volume != Mute) {
                        target.Volume = Source.volume;
                        Source.volume = Mute;
                    }
                    AudioSettings.GetDSPBufferSize(out int BufferSize, out int _);
                    if (Math.Abs(target.timeSamples - Source.timeSamples) > BufferSize) {
                        target.timeSamples = Source.timeSamples;
                    }
                    if (!decompressed) {
                        target.Clip = null;
                    }
                }
                target.VolumeRolloff = Source.rolloffMode switch {
                    AudioRolloffMode.Linear => Rolloffs.Linear,
                    AudioRolloffMode.Logarithmic => Rolloffs.Logarithmic,
                    _ => Rolloffs.Disabled,
                };
            } else {
                if (target) {
                    Destroy(target);
                }
                Destroy(this);
            }
        }
    }
}