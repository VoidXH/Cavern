﻿using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern {
    /// <summary>An audio object in 3D space, in stereo, or both.</summary>
    public partial class Source {
        // ------------------------------------------------------------------
        // Audio clip settings
        // ------------------------------------------------------------------
        /// <summary>The audio clip to play.</summary>
        public Clip Clip;
        /// <summary>Continue playback of the source.</summary>
        public bool IsPlaying = true;
        /// <summary>Restart the source when finished.</summary>
        public bool Loop = false;
        /// <summary>Mute the source.</summary>
        public bool Mute = false;
        /// <summary>Only mix this channel to subwoofers.</summary>
        public bool LFE = false;

        // ------------------------------------------------------------------
        // 2D processing
        // ------------------------------------------------------------------
        /// <summary>Source playback volume.</summary>
        public float Volume = 1;
        /// <summary>Playback speed with no pitch correction.</summary>
        public float Pitch = 1;
        /// <summary>Balance between left and right channels.</summary>
        public float stereoPan = 0;

        // ------------------------------------------------------------------
        // 3D processing
        // ------------------------------------------------------------------
        /// <summary>Balance between 2D and 3D mixing. 0 is 2D and 1 is 3D.</summary>
        public float SpatialBlend = 1;
        /// <summary>Audio source size relative to <see cref="Listener.EnvironmentSize"/>. 0 is a point, 1 is the entire room.</summary>
        public float Size = 0;
        /// <summary>Doppler effect scale, 1 is real.</summary>
        public float DopplerLevel = 0;
        /// <summary>Volume decreasing function by distance.</summary>
        public Rolloffs VolumeRolloff = Rolloffs.Logarithmic;
        /// <summary>Filter to be applied on the 3D mixed output.</summary>
        public Filter SpatialFilter;

        // ------------------------------------------------------------------
        // Variables
        // ------------------------------------------------------------------
        /// <summary>Object position in absolute space.</summary>
        public Vector Position;
        /// <summary>Clip playback position in samples.</summary>
        public int TimeSamples = 0;

        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------
        /// <summary>Last rendered samples from this source or the last samples generated by overriding <see cref="GetSamples"/>.</summary>
        public float[][] Rendered;
        /// <summary>Indicates that the source meets rendering requirements, and <see cref="GetSamples"/> won't fail.</summary>
        protected internal virtual bool Renderable => IsPlaying && Clip;

        // ------------------------------------------------------------------
        // Public functions
        // ------------------------------------------------------------------
        /// <summary>Start playback from the beginning of the <see cref="Clip"/>.</summary>
        /// <param name="delaySamples">Optional delay in samples</param>
        public void Play(long delaySamples = 0) {
            TimeSamples = 0;
            delay = delaySamples;
            IsPlaying = true;
        }

        /// <summary>Start playback from the beginning after the given time.</summary>
        /// <param name="seconds">Delay in seconds</param>
        public void PlayDelayed(float seconds) => Play((long)(seconds * Clip.SampleRate));

        /// <summary>Jump to a random position.</summary>
        public void RandomPosition() => TimeSamples = random.Next(0, Clip.Samples);

        /// <summary>Pause playback if it's not paused.</summary>
        public void Pause() => IsPlaying = false;

        /// <summary>Continue playback if it's paused.</summary>
        public void UnPause() => IsPlaying = true;

        /// <summary>Toggle between playback and pause.</summary>
        public void TogglePlay() => IsPlaying = !IsPlaying;

        /// <summary>Pause playback and reset position. The next <see cref="UnPause"/> will start playback from the beginning.</summary>
        public void Stop() {
            IsPlaying = false;
            TimeSamples = 0;
        }

        /// <summary>Copy the settings of another <see cref="Source"/>.</summary>
        /// <param name="from">Target source</param>
        public void CopySettings(Source from) {
            Clip = from.Clip; IsPlaying = from.IsPlaying; Loop = from.Loop; Mute = from.Mute; LFE = from.LFE; Volume = from.Volume;
            Pitch = from.Pitch; stereoPan = from.stereoPan; SpatialBlend = from.SpatialBlend; DopplerLevel = from.DopplerLevel;
            VolumeRolloff = from.VolumeRolloff;
        }

        /// <summary>Add a new <see cref="SpatialFilter"/> to this source.</summary>
        public void AddFilter(Filter target) {
            if (SpatialFilter == null)
                SpatialFilter = target;
            else {
                if (!(SpatialFilter is ComplexFilter)) {
                    Filter old = SpatialFilter;
                    SpatialFilter = new ComplexFilter();
                    ((ComplexFilter)SpatialFilter).Filters.Add(old);
                }
                ((ComplexFilter)SpatialFilter).Filters.Add(target);
            }
        }

        /// <summary>Remove a <see cref="SpatialFilter"/> from this source.</summary>
        public void RemoveFilter(Filter target) {
            if (SpatialFilter == target)
                SpatialFilter = null;
            else {
                ComplexFilter complex = (ComplexFilter)SpatialFilter;
                if (complex.Filters.Count == 1 && complex.Filters[0] == target)
                    SpatialFilter = null;
                else
                    complex.Filters.Remove(target);
            }
        }

        /// <summary>Implicit null check.</summary>
        public static implicit operator bool(Source source) => source != null;
    }
}