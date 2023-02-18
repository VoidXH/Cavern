using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Filters;
using Cavern.Helpers;
using Cavern.Utilities;

namespace Cavern {
    /// <summary>
    /// An audio object in 3D space, in stereo, or both.
    /// </summary>
    [AddComponentMenu("Audio/3D Audio Source")]
    public class AudioSource3D : MonoBehaviour {
        // ------------------------------------------------------------------
        // Audio source settings
        // ------------------------------------------------------------------
        /// <summary>
        /// The audio clip to play. If given, it will be converted to Cavern's <see cref="Clip"/> format.
        /// </summary>
        [Header("Audio clip settings")]
        [Tooltip("The audio clip to play.")]
        public AudioClip Clip;

        /// <summary>
        /// The audio clip to play in Cavern's format. Overrides <see cref="Clip"/>.
        /// </summary>
        public Clip Clip3D;

        /// <summary>
        /// Continue playback of the source.
        /// </summary>
        [Tooltip("Continue playback of the source.")]
        public bool IsPlaying = true;

        /// <summary>
        /// Restart the source when finished.
        /// </summary>
        [Tooltip("Restart the source when finished.")]
        public bool Loop;

        /// <summary>
        /// Mute the source.
        /// </summary>
        [Tooltip("Mute the source.")]
        public bool Mute;

        /// <summary>
        /// Only mix this channel to subwoofers.
        /// </summary>
        [Tooltip("Only mix this channel to subwoofers.")]
        public bool LFE;

        /// <summary>
        /// Start playback from a random position.
        /// </summary>
        [Tooltip("Start playback from a random position.")]
        public bool RandomPosition;

        /// <summary>
        /// Source playback volume.
        /// </summary>
        [Header("1D processing")]
        [Tooltip("Source playback volume.")]
        [Range(0, 1)] public float Volume = 1;

        /// <summary>
        /// Playback speed with no pitch correction.
        /// </summary>
        [Tooltip("Playback speed with no pitch correction.")]
        [Range(.5f, 3)] public float Pitch = 1;

        /// <summary>
        /// Balance between left and right channels.
        /// </summary>
        [Tooltip("Balance between left and right channels.")]
        [Range(-1, 1)] public float StereoPan;

        /// <summary>
        /// Balance between 1D and 3D mixing. 0 is 1D and 1 is 3D.
        /// </summary>
        [Header("3D processing")]
        [Tooltip("Stereo mix <-> 3D mix")]
        [Range(0, 1)] public float SpatialBlend = 1;

        /// <summary>
        /// Audio source size relative to <see cref="Listener.EnvironmentSize"/>. 0 is a point, 1 is the entire room.
        /// </summary>
        [Tooltip("Audio source size relative to the environment. 0 is a point, 1 is the entire room.")]
        [Range(0, 1)] public float Size;

        /// <summary>
        /// Doppler effect scale, 1 is real.
        /// </summary>
        [Tooltip("Doppler effect scale, 1 is real.")]
        [Range(0, 5)] public float DopplerLevel;

        /// <summary>
        /// Volume decreasing function by distance.
        /// </summary>
        [Tooltip("Volume decreasing function by distance.")]
        public Rolloffs VolumeRolloff = Rolloffs.Logarithmic;

        /// <summary>
        /// Filter to be applied on the 3D mixed output.
        /// </summary>
        [Tooltip("Filter to be applied on the 3D mixed output.")]
        public Filter SpatialFilter;

        /// <summary>
        /// Simulates distance, not just direction when using virtualization.
        /// </summary>
        [Tooltip("Simulates distance, not just direction when using virtualization.")]
        public bool DistanceSimulation;

#pragma warning disable IDE1006 // Naming Styles
        // ------------------------------------------------------------------
        // Unity variable name aliases
        // ------------------------------------------------------------------
        /// <summary>
        /// Alias for <see cref="Clip"/>.
        /// </summary>
        public AudioClip clip {
            get => Clip;
            set => Clip = value;
        }

        /// <summary>
        /// Alias for <see cref="DopplerLevel"/>.
        /// </summary>
        public float dopplerLevel {
            get => DopplerLevel;
            set => DopplerLevel = value;
        }

        /// <summary>
        /// Alias for <see cref="IsPlaying"/>.
        /// </summary>
        public bool isPlaying {
            get => IsPlaying;
            set => IsPlaying = value;
        }

        /// <summary>
        /// Alias for <see cref="Loop"/>.
        /// </summary>
        public bool loop {
            get => Loop;
            set => Loop = value;
        }

        /// <summary>
        /// Alias for <see cref="Mute"/>.
        /// </summary>
        public bool mute {
            get => Mute;
            set => Mute = value;
        }

        /// <summary>
        /// Alias for <see cref="StereoPan"/>.
        /// </summary>
        public float panStereo {
            get => StereoPan;
            set => StereoPan = value;
        }

        /// <summary>
        /// Alias for <see cref="Pitch"/>.
        /// </summary>
        public float pitch {
            get => Pitch;
            set => Pitch = value;
        }

        /// <summary>
        /// Alias for <see cref="SpatialBlend"/>.
        /// </summary>
        public float spatialBlend {
            get => SpatialBlend;
            set => SpatialBlend = value;
        }

        /// <summary>
        /// Alias for <see cref="Volume"/>.
        /// </summary>
        public float volume {
            get => Volume;
            set => Volume = value;
        }

        /// <summary>
        /// Clip playback position in seconds.
        /// </summary>
        public float time {
            get {
                if (cavernSource.Clip) {
                    return timeSamples / (float)cavernSource.Clip.SampleRate;
                } else {
                    return 0;
                }
            }
            set {
                if (cavernSource.Clip) {
                    timeSamples = (int)(value * cavernSource.Clip.SampleRate);
                }
            }
        }

        /// <summary>
        /// Clip playback position in samples.
        /// </summary>
        public int timeSamples {
            get => cavernSource.TimeSamples;
            set => cavernSource.TimeSamples = value;
        }
#pragma warning restore IDE1006 // Naming Styles

        // ------------------------------------------------------------------
        // Public properties
        // ------------------------------------------------------------------
        /// <summary>
        /// True if the source has an active clip.
        /// </summary>
        public bool HasClip => Clip != null || Clip3D != null;

        /// <summary>
        /// Sample count for a single channel or -1 if there's no active clip.
        /// </summary>
        public int Samples {
            get {
                if (Clip3D) {
                    return Clip3D.Samples;
                }
                if (Clip) {
                    return Clip.samples;
                }
                return -1;
            }
        }

        /// <summary>
        /// Sample rate or -1 if there's no active clip.
        /// </summary>
        public int SampleRate {
            get {
                if (Clip3D) {
                    return Clip3D.SampleRate;
                }
                if (Clip) {
                    return Clip.frequency;
                }
                return -1;
            }
        }

        // ------------------------------------------------------------------
        // Public functions
        // ------------------------------------------------------------------
        /// <summary>
        /// Start playback from the beginning of the <see cref="clip"/> immediately.
        /// </summary>
        public void Play() {
            cavernSource.Play();
            IsPlaying = cavernSource.IsPlaying;
        }

        /// <summary>
        /// Start playback from the beginning of the <see cref="clip"/> after a delay has passed.
        /// </summary>
        /// <param name="delaySamples">Optional delay in samples</param>
        public void Play(long delaySamples) {
            cavernSource.Play(delaySamples);
            IsPlaying = cavernSource.IsPlaying;
        }

        /// <summary>
        /// Start playback from the beginning after the given time.
        /// </summary>
        /// <param name="seconds">Delay in seconds</param>
        public void PlayDelayed(float seconds) {
            cavernSource.PlayDelayed(seconds);
            IsPlaying = cavernSource.IsPlaying;
        }

        /// <summary>
        /// Pause playback if it's not paused.
        /// </summary>
        public void Pause() {
            cavernSource.Pause();
            IsPlaying = cavernSource.IsPlaying;
        }

        /// <summary>
        /// Continue playback if it's paused.
        /// </summary>
        public void UnPause() {
            cavernSource.UnPause();
            IsPlaying = cavernSource.IsPlaying;
        }

        /// <summary>
        /// Toggle between playback and pause.
        /// </summary>
        public void TogglePlay() {
            cavernSource.TogglePlay();
            IsPlaying = cavernSource.IsPlaying;
        }

        /// <summary>
        /// Pause playback and reset position. The next <see cref="UnPause"/> will start playback from the beginning.
        /// </summary>
        public void Stop() {
            cavernSource.Stop();
            IsPlaying = cavernSource.IsPlaying;
        }

        /// <summary>
        /// Play a clip once on full volume from this <see cref="GameObject"/>.
        /// </summary>
        /// <param name="clip">Target clip</param>
        public void PlayOneShot(AudioClip clip) => PlayOneShot(clip, 1, false);

        /// <summary>
        /// Play a clip once with a custom <paramref name="volume"/> from this <see cref="GameObject"/>.
        /// </summary>
        /// <param name="clip">Target clip</param>
        /// <param name="volume">Playback volume</param>
        public void PlayOneShot(AudioClip clip, float volume) => PlayOneShot(clip, volume, false);

        /// <summary>
        /// Play a clip once.
        /// </summary>
        /// <param name="clip">Target clip</param>
        /// <param name="volume">Playback volume</param>
        /// <param name="isStatic">Do not play on the source's game object, play at the source's current position instead.</param>
        public void PlayOneShot(AudioClip clip, float volume, bool isStatic) {
            GameObject obj;
            if (isStatic) {
                obj = new GameObject("Temporary Static Audio Source");
                obj.transform.position = transform.position;
            } else {
                obj = gameObject;
            }
            AudioSource3D source = obj.AddComponent<AudioSource3D>();
            source.CopySettings(this);
            source.Clip = clip;
            source.IsPlaying = true;
            source.Loop = source.Mute = source.RandomPosition = false;
            source.Volume = volume;
            OneShotDestructor.Constructor(obj, source, isStatic);
        }

        /// <summary>
        /// Copy the settings of another <see cref="AudioSource3D"/>.
        /// </summary>
        /// <param name="from">Target source</param>
        public void CopySettings(AudioSource3D from) {
            Clip = from.Clip;
            Clip3D = from.Clip3D;
            IsPlaying = from.IsPlaying;
            Loop = from.Loop;
            Mute = from.Mute;
            LFE = from.LFE;
            Volume = from.Volume;
            Pitch = from.Pitch;
            StereoPan = from.StereoPan;
            SpatialBlend = from.SpatialBlend;
            Size = from.Size;
            DopplerLevel = from.DopplerLevel;
            VolumeRolloff = from.VolumeRolloff;
            SpatialFilter = from.SpatialFilter;
            DistanceSimulation = from.DistanceSimulation;
            timeSamples = from.timeSamples;
        }

        /// <summary>
        /// Add a new <see cref="SpatialFilter"/> to this source.
        /// </summary>
        public void AddFilter(Filter target) {
            cavernSource.AddFilter(target);
            SpatialFilter = cavernSource.SpatialFilter;
        }

        /// <summary>
        /// Remove a <see cref="SpatialFilter"/> from this source.
        /// </summary>
        public void RemoveFilter(Filter target) {
            cavernSource.RemoveFilter(target);
            SpatialFilter = cavernSource.SpatialFilter;
        }

        /// <summary>
        /// Play a clip once at the given world position with full volume.
        /// </summary>
        /// <param name="clip">Target clip</param>
        /// <param name="position">World position of the clip</param>
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position) => PlayClipAtPoint(clip, position, 1);

        /// <summary>
        /// Play a clip once at the given world position with a custom <paramref name="volume"/>.
        /// </summary>
        /// <param name="clip">Target clip</param>
        /// <param name="position">World position of the clip</param>
        /// <param name="volume">Playback volume</param>
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume) {
            GameObject obj = new GameObject("Temporary Static Audio Source");
            obj.transform.position = position;
            AudioSource3D source = obj.AddComponent<AudioSource3D>();
            source.Clip = clip;
            source.IsPlaying = true;
            source.Volume = volume;
            OneShotDestructor.Constructor(obj, source, true);
        }

        // ------------------------------------------------------------------
        // Private vars
        // ------------------------------------------------------------------
        /// <summary>
        /// Cavern source handled by this component.
        /// </summary>
        protected internal Source cavernSource = new Source();

        /// <summary>
        /// Cached value of <see cref="Source.IsPlaying"/>, to prevent overriding auto-stops and playback functions.
        /// </summary>
        bool internalPlayState = true;

        /// <summary>
        /// Hash code of the last imported <see cref="AudioClip"/> that has been converted to <see cref="Cavern.Clip"/>.
        /// </summary>
        int lastClipHash;

        // ------------------------------------------------------------------
        // Lifecycle helpers
        // ------------------------------------------------------------------
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDrawGizmos() => Gizmos.DrawIcon(transform.position, "AudioSource Gizmo", true);

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() => AudioListener3D.cavernListener.AttachSource(cavernSource);

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() => AudioListener3D.cavernListener.DetachSource(cavernSource);

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() => Update();

        void Update() => SourceUpdate();

        /// <summary>
        /// Synchronize this interface with <see cref="cavernSource"/>.
        /// </summary>
        protected void SourceUpdate() {
            cavernSource.Position = VectorUtils.VectorMatch(transform.position);
            Tunneler.TunnelClips(ref cavernSource.Clip, Clip, Clip3D, ref lastClipHash);
            if (cavernSource.IsPlaying == internalPlayState) {
                cavernSource.IsPlaying = IsPlaying;
            } else {
                internalPlayState = IsPlaying = cavernSource.IsPlaying;
            }
            cavernSource.Loop = Loop;
            cavernSource.Mute = Mute;
            cavernSource.LFE = LFE;
            if (RandomPosition) {
                cavernSource.RandomPosition();
                RandomPosition = false;
            }
            cavernSource.Volume = Volume;
            cavernSource.Pitch = Pitch;
            cavernSource.stereoPan = StereoPan;
            cavernSource.SpatialBlend = SpatialBlend;
            cavernSource.Size = Size;
            cavernSource.DopplerLevel = DopplerLevel;
            cavernSource.VolumeRolloff = VolumeRolloff;
            cavernSource.SpatialFilter = SpatialFilter;
            cavernSource.DistanceSimulation = DistanceSimulation;
        }
    }
}