using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Channels;
using Cavern.Utilities;

namespace Cavern.Cavernize {
    /// <summary>
    /// Adds height to each channel of a regular surround mix.
    /// </summary>
    [AddComponentMenu("Audio/Cavernize/3D Conversion")]
    public class Cavernizer : MonoBehaviour {
        /// <summary>
        /// The audio clip to convert.
        /// </summary>
        [Tooltip("The audio clip to convert.")]
        public AudioClip Clip;

        /// <summary>
        /// The audio clip to convert in Cavern's format. Overrides <see cref="Clip"/>.
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
        /// How many times the object positions are calculated every second.
        /// </summary>
        [Tooltip("How many times the object positions are calculated every second.")]
        public int UpdatesPerSecond = 200;

        /// <summary>
        /// Source playback volume.
        /// </summary>
        [Tooltip("Source playback volume.")]
        [Range(0, 1)] public float Volume = 1;

        /// <summary>
        /// 3D audio effect strength.
        /// </summary>
        [Tooltip("3D audio effect strength.")]
        [Range(0, 1)] public float Effect = .75f;

        /// <summary>
        /// Smooth object movements.
        /// </summary>
        [Tooltip("Smooth object movements.")]
        [Range(0, 1)] public float Smoothness = .8f;

        /// <summary>
        /// Creates missing channels from existing ones. Works best if the source is matrix-encoded. Not recommended for Gaming 3D setups.
        /// </summary>
        [Header("Matrix Upmix")]
        [Tooltip("Creates missing channels from existing ones. Works best if the source is matrix-encoded. " +
            "Not recommended for Gaming 3D setups.")]
        public bool MatrixUpmix = true;

        /// <summary>
        /// Don't spatialize the front channel. This can fix the speech from above anomaly if it's present.
        /// </summary>
        [Header("Spatializer")]
        [Tooltip("Don't spatialize the front channel. This can fix the speech from above anomaly if it's present.")]
        public bool CenterStays = true;

        /// <summary>
        /// Keep all frequencies below this on the ground.
        /// </summary>
        [Tooltip("Keep all frequencies below this on the ground.")]
        public float GroundCrossover = 250;

        /// <summary>
        /// Show converted objects.
        /// </summary>
        [Header("Debug")]
        [Tooltip("Show converted objects.")]
        public bool Visualize;

#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// Playback position in seconds.
        /// </summary>
        public float time {
            get => timeSamples / (float)AudioListener3D.Current.SampleRate;
            set => timeSamples = (int)(value * AudioListener3D.Current.SampleRate);
        }
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Playback position in samples.
        /// </summary>
        public int timeSamples;

        /// <summary>
        /// This height value indicates if a channel is skipped in height processing.
        /// </summary>
        internal const float unsetHeight = -2;

        /// <summary>
        /// <see cref="AudioListener3D.UpdateRate"/> for conversion.
        /// </summary>
        int updateRate;

        /// <summary>
        /// Cached <see cref="AudioListener3D.SampleRate"/> as the listener is reconfigured for the Cavernize process.
        /// </summary>
        int oldSampleRate;

        /// <summary>
        /// Cached <see cref="AudioListener3D.UpdateRate"/> as the listener is reconfigured for the Cavernize process.
        /// </summary>
        int oldUpdateRate;

        internal Dictionary<ReferenceChannel, SpatializedChannel> channels = new Dictionary<ReferenceChannel, SpatializedChannel>();
        internal SpatializedChannel this[int index] => channels[(ReferenceChannel)index]; // This is horribly hacky and will be removed

        /// <summary>
        /// The 5.1/7.1 stream generator.
        /// </summary>
        SurroundUpmixer generator;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() {
            AudioListener3D listener = AudioListener3D.Current;
            oldSampleRate = listener.SampleRate;
            oldUpdateRate = listener.UpdateRate;

            if (Clip) {
                Clip3D = AudioClip3D.FromUnityClip(Clip);
            }
            updateRate = listener.UpdateRate = (listener.SampleRate = Clip3D.SampleRate) / UpdatesPerSecond;
            generator = new SurroundUpmixer(Clip3D);
            generator.OnPlaybackFinished += () => IsPlaying = false;

            ReferenceChannel[] targetChannels = generator.GetChannels();
            for (int source = 0; source < targetChannels.Length; ++source) {
                channels[targetChannels[source]] = new SpatializedChannel(targetChannels[source], this, updateRate);
            }
        }

        internal SpatializedChannel GetChannel(ReferenceChannel target) {
            if (channels.ContainsKey(target)) {
                return channels[target];
            }
            return null;
        }

        void GenerateSampleBlock() {
            AudioListener3D listener = AudioListener3D.Current;
            float smoothFactor = 1f - QMath.Lerp(updateRate, listener.SampleRate,
                (float)Math.Pow(Smoothness, .1f)) / listener.SampleRate * .999f;
            if (IsPlaying) {
                generator.loop = Loop;
                generator.timeSamples = timeSamples;
                generator.GenerateSamples(updateRate);
                // This nasty wrapping will also be removed once Cavernize is moved away from the Unity DLL
                timeSamples = generator.timeSamples;
                foreach (KeyValuePair<ReferenceChannel, SpatializedChannel> channel in channels) {
                    channel.Value.WrittenOutput = generator.Readable(channel.Key);
                    float[] source = generator.RetrieveSamples(channel.Key);
                    Array.Copy(source, channel.Value.Output, source.Length);
                }
            }
            // Overwrite channel data with new output, even if it's empty
            foreach (KeyValuePair<ReferenceChannel, SpatializedChannel> channel in channels) {
                channel.Value.Tick(Effect, smoothFactor, GroundCrossover, Visualize);
            }
        }

        internal MultichannelWaveform Tick(SpatializedChannel source, bool groundLevel) {
            if (source.TicksTook == 2) { // Both moving and ground source was fed
                GenerateSampleBlock();
                foreach (KeyValuePair<ReferenceChannel, SpatializedChannel> channel in channels) {
                    channel.Value.TicksTook = 0;
                }
            }
            ++source.TicksTook;
            return new MultichannelWaveform(source.GetOutput(groundLevel));
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDestroy() {
            foreach (KeyValuePair<ReferenceChannel, SpatializedChannel> channel in channels) {
                channel.Value.Destroy();
            }
            AudioListener3D listener = AudioListener3D.Current;
            listener.SampleRate = oldSampleRate;
            listener.UpdateRate = oldUpdateRate;
        }
    }
}