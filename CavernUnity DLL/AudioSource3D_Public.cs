using System;
using UnityEngine;

using Cavern.Filters;
using Cavern.Helpers;

namespace Cavern {
    /// <summary>An audio object in 3D space, in stereo, or both.</summary>
    public partial class AudioSource3D : MonoBehaviour {
        // ------------------------------------------------------------------
        // Audio source settings
        // ------------------------------------------------------------------
        /// <summary>The audio clip to play.</summary>
        [Header("Audio clip settings")]
        [Tooltip("The audio clip to play.")]
        public AudioClip Clip;
        /// <summary>Continue playback of the source.</summary>
        [Tooltip("Continue playback of the source.")]
        public bool IsPlaying = true;
        /// <summary>Restart the source when finished.</summary>
        [Tooltip("Restart the source when finished.")]
        public bool Loop = false;
        /// <summary>Mute the source.</summary>
        [Tooltip("Mute the source.")]
        public bool Mute = false;
        /// <summary>Only mix this channel to subwoofers.</summary>
        [Tooltip("Only mix this channel to subwoofers.")]
        public bool LFE = false;
        /// <summary>Start playback from a random position.</summary>
        [Tooltip("Start playback from a random position.")]
        public bool RandomPosition = false;

        /// <summary>Source playback volume.</summary>
        [Header("2D processing")]
        [Tooltip("Source playback volume.")]
        [Range(0, 1)] public float Volume = 1;
        /// <summary>Playback speed with no pitch correction.</summary>
        [Tooltip("Playback speed with no pitch correction.")]
        [Range(.5f, 3)] public float Pitch = 1;
        /// <summary>Balance between left and right channels.</summary>
        [Tooltip("Balance between left and right channels.")]
        [Range(-1, 1)] public float StereoPan = 0;

        /// <summary>Balance between 2D and 3D mixing. 0 is 2D and 1 is 3D.</summary>
        [Header("3D processing")]
        [Tooltip("Stereo mix <-> 3D mix")]
        [Range(0, 1)] public float SpatialBlend = 1;
        /// <summary>Audio source size relative to the <see cref="AudioListener3D.EnvironmentSize"/>. 0 is a point, 1 is the entire room.</summary>
        [Tooltip("Audio source size relative to the environment. 0 is a point, 1 is the entire room.")]
        [Range(0, 1)] public float Size = 0;
        /// <summary>Doppler effect scale, 1 is real.</summary>
        [Tooltip("Doppler effect scale, 1 is real.")]
        [Range(0, 5)] public float DopplerLevel = 0;
        /// <summary>Volume decreasing function by distance.</summary>
        [Tooltip("Volume decreasing function by distance.")]
        public Rolloffs VolumeRolloff = Rolloffs.Logarithmic;
        /// <summary>Filter to be applied on the 3D mixed output.</summary>
        [Tooltip("Filter to be applied on the 3D mixed output.")]
        public Filter SpatialFilter;

        // ------------------------------------------------------------------
        // Variables
        // ------------------------------------------------------------------
        /// <summary>Clip playback position in samples.</summary>
        [NonSerialized] public int timeSamples = 0;

        // ------------------------------------------------------------------
        // Unity variable name aliases
        // ------------------------------------------------------------------
        /// <summary>Alias for <see cref="Clip"/>.</summary>
        public AudioClip clip {
            get => Clip;
            set => Clip = value;
        }

        /// <summary>Alias for <see cref="DopplerLevel"/>.</summary>
        public float dopplerLevel {
            get => DopplerLevel;
            set => DopplerLevel = value;
        }

        /// <summary>Alias for <see cref="IsPlaying"/>.</summary>
        public bool isPlaying {
            get => IsPlaying;
            set => IsPlaying = value;
        }

        /// <summary>Alias for <see cref="Loop"/>.</summary>
        public bool loop {
            get => Loop;
            set => Loop = value;
        }

        /// <summary>Alias for <see cref="Mute"/>.</summary>
        public bool mute {
            get => Mute;
            set => Mute = value;
        }

        /// <summary>Alias for <see cref="StereoPan"/>.</summary>
        public float panStereo {
            get => StereoPan;
            set => StereoPan = value;
        }

        /// <summary>Alias for <see cref="Pitch"/>.</summary>
        public float pitch {
            get => Pitch;
            set => Pitch = value;
        }

        /// <summary>Alias for <see cref="SpatialBlend"/>.</summary>
        public float spatialBlend {
            get => SpatialBlend;
            set => SpatialBlend = value;
        }

        /// <summary>Alias for <see cref="Volume"/>.</summary>
        public float volume {
            get => Volume;
            set => Volume = value;
        }

        /// <summary>Clip playback position in seconds.</summary>
        public float time {
            get {
                if (Clip)
                    return timeSamples / (float)Clip.frequency;
                else
                    return 0;
            }
            set {
                if (Clip)
                    timeSamples = (int)(value * Clip.frequency);
            }
        }

        // ------------------------------------------------------------------
        // Public functions
        // ------------------------------------------------------------------
        /// <summary>Start playback from the beginning of the <see cref="clip"/>.</summary>
        /// <param name="DelaySamples">Optional delay in samples</param>
        public void Play(ulong DelaySamples = 0) {
            timeSamples = 0;
            Delay = DelaySamples;
            IsPlaying = true;
        }

        /// <summary>Start playback from the beginning after the given time.</summary>
        /// <param name="Seconds">Delay in seconds</param>
        public void PlayDelayed(float Seconds) {
            Play();
            if (Clip)
                Delay = (ulong)Seconds * (ulong)Clip.frequency;
        }

        /// <summary>Pause playback if it's not paused.</summary>
        public void Pause() => IsPlaying = false;

        /// <summary>Continue playback if it's paused.</summary>
        public void UnPause() => IsPlaying = true;

        /// <summary>Toggle between playback and pause.</summary>
        public void TogglePlay() => IsPlaying = !IsPlaying;

        /// <summary>Pause playback and reset position. The next <see cref="UnPause"/> will start playback from the beginning.</summary>
        public void Stop() {
            IsPlaying = false;
            timeSamples = 0;
        }

        /// <summary>Play a clip once.</summary>
        /// <param name="Clip">Target clip</param>
        /// <param name="Volume">Playback volume</param>
        /// <param name="Static">Do not play on the source's game object, play at the source's current position instead.</param>
        public void PlayOneShot(AudioClip Clip, float Volume = 1, bool Static = false) {
            GameObject Obj;
            if (Static) {
                Obj = new GameObject("Temporary Static Audio Source");
                Obj.transform.position = transform.position;
            } else
                Obj = gameObject;
            AudioSource3D Source = Obj.AddComponent<AudioSource3D>();
            Source.CopySettings(this);
            Source.Clip = Clip;
            Source.IsPlaying = true;
            Source.Loop = Source.Mute = Source.RandomPosition = false;
            Source.Volume = Volume;
            OneShotDestructor.Constructor(Obj, Source, Static);
        }

        /// <summary>Copy the settings of another <see cref="AudioSource3D"/>.</summary>
        /// <param name="From">Target source</param>
        public void CopySettings(AudioSource3D From) {
            Clip = From.Clip; IsPlaying = From.IsPlaying; Loop = From.Loop;
            Mute = From.Mute; LFE = From.LFE; RandomPosition = From.RandomPosition;
            Volume = From.Volume; Pitch = From.Pitch; StereoPan = From.StereoPan;
            SpatialBlend = From.SpatialBlend; DopplerLevel = From.DopplerLevel;
            VolumeRolloff = From.VolumeRolloff;
        }

        /// <summary>Add a new <see cref="SpatialFilter"/> to this source.</summary>
        public void AddFilter(Filter Target) {
            if (SpatialFilter == null)
                SpatialFilter = Target;
            else {
                if (!(SpatialFilter is ComplexFilter)) {
                    Filter Old = SpatialFilter;
                    SpatialFilter = new ComplexFilter();
                    ((ComplexFilter)SpatialFilter).Filters.Add(Old);
                }
                ((ComplexFilter)SpatialFilter).Filters.Add(Target);
            }
        }

        /// <summary>Remove a <see cref="SpatialFilter"/> from this source.</summary>
        public void RemoveFilter(Filter Target) {
            if (SpatialFilter == Target)
                SpatialFilter = null;
            else {
                ComplexFilter Complex = (ComplexFilter)SpatialFilter;
                if (Complex.Filters.Count == 1 && Complex.Filters[0] == Target)
                    SpatialFilter = null;
                else
                    Complex.Filters.Remove(Target);
            }
        }

        /// <summary>Play a clip once at the given world position.</summary>
        /// <param name="Clip">Target clip</param>
        /// <param name="Position">World position of the clip</param>
        /// <param name="Volume">Playback volume</param>
        public static void PlayClipAtPoint(AudioClip Clip, Vector3 Position, float Volume = 1) {
            GameObject Obj = new GameObject("Temporary Static Audio Source");
            Obj.transform.position = Position;
            AudioSource3D Source = Obj.AddComponent<AudioSource3D>();
            Source.Clip = Clip;
            Source.IsPlaying = true;
            Source.Volume = Volume;
            OneShotDestructor.Constructor(Obj, Source, true);
        }
    }
}