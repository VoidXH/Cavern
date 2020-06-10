using System;
using System.Collections.Generic;
using UnityEngine;

using Cavern.Remapping;
using Cavern.Utilities;

namespace Cavern.Cavernize {
    /// <summary>Adds height to each channel of a regular surround mix.</summary>
    [AddComponentMenu("Audio/Cavernize/3D Conversion")]
    public class Cavernizer : MonoBehaviour {
        /// <summary>The audio clip to convert.</summary>
        [Tooltip("The audio clip to convert.")]
        public AudioClip Clip;
        /// <summary>Continue playback of the source.</summary>
        [Tooltip("Continue playback of the source.")]
        public bool IsPlaying = true;
        /// <summary>Restart the source when finished.</summary>
        [Tooltip("Restart the source when finished.")]
        public bool Loop = false;
        /// <summary>How many times the object positions are calculated every second.</summary>
        [Tooltip("How many times the object positions are calculated every second.")]
        public int UpdatesPerSecond = 200;
        /// <summary>Source playback volume.</summary>
        [Tooltip("Source playback volume.")]
        [Range(0, 1)] public float Volume = 1;
        /// <summary>3D audio effect strength.</summary>
        [Tooltip("3D audio effect strength.")]
        [Range(0, 1)] public float Effect = .75f;
        /// <summary>Smooth object movements.</summary>
        [Tooltip("Smooth object movements.")]
        [Range(0, 1)] public float Smoothness = .8f;

        /// <summary>Creates missing channels from existing ones. Works best if the source is matrix-encoded. Not recommended for Gaming 3D setups.</summary>
        [Header("Matrix Upmix")]
        [Tooltip("Creates missing channels from existing ones. Works best if the source is matrix-encoded. Not recommended for Gaming 3D setups.")]
        public bool MatrixUpmix = true;

        /// <summary>Don't spatialize the front channel. This can fix the speech from above anomaly if it's present.</summary>
        [Header("Spatializer")]
        [Tooltip("Don't spatialize the front channel. This can fix the speech from above anomaly if it's present.")]
        public bool CenterStays = true;
        /// <summary>Keep all frequencies below this on the ground.</summary>
        [Tooltip("Keep all frequencies below this on the ground.")]
        public float GroundCrossover = 250;

        /// <summary>Show converted objects.</summary>
        [Header("Debug")]
        [Tooltip("Show converted objects.")]
        public bool Visualize = false;

#pragma warning disable IDE1006 // Naming Styles
        /// <summary>Playback position in seconds.</summary>
        public float time {
            get => timeSamples / (float)AudioListener3D.Current.SampleRate;
            set => timeSamples = (int)(value * AudioListener3D.Current.SampleRate);
        }
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>Playback position in samples.</summary>
        public int timeSamples;

        /// <summary>This height value indicates if a channel is skipped in height processing.</summary>
        internal const float unsetHeight = -2;

        /// <summary>Imported audio data.</summary>
        float[] clipSamples;
        /// <summary>Channel count of <see cref="Clip"/>.</summary>
        int clipChannels;
        /// <summary>Length of <see cref="Clip"/> in samples/channel.</summary>
        int clipLength;
        /// <summary><see cref="AudioListener3D.UpdateRate"/> for conversion.</summary>
        int updateRate;
        /// <summary>Cached <see cref="AudioListener3D.SampleRate"/> as the listener is reconfigured for the Cavernize process.</summary>
        int oldSampleRate;
        /// <summary>Cached <see cref="AudioListener3D.UpdateRate"/> as the listener is reconfigured for the Cavernize process.</summary>
        int oldUpdateRate;
        /// <summary>The channels for a base 7.1 layout.</summary>
        internal readonly SpatializedChannel[] mains = new SpatializedChannel[8];

        // TODO: 9-16 CH WAV import

        internal Dictionary<ChannelPrototype, SpatializedChannel> channels = new Dictionary<ChannelPrototype, SpatializedChannel>();

        /// <summary>Possible upmix targets, always created.</summary>
        static readonly ChannelPrototype[] UpmixTargets = { ChannelPrototype.FrontCenter, ChannelPrototype.SideLeft, ChannelPrototype.SideRight,
            ChannelPrototype.RearLeft, ChannelPrototype.RearRight };

        void Start() {
            AudioListener3D listener = AudioListener3D.Current;
            oldSampleRate = listener.SampleRate;
            oldUpdateRate = listener.UpdateRate;
            listener.SampleRate = Clip.frequency;
            updateRate = listener.UpdateRate = Clip.frequency / UpdatesPerSecond;
            if (Clip.samples > updateRate)
                clipLength = Clip.samples;
            else
                clipLength = updateRate;
            int sampleCount = (clipChannels = Clip.channels) * clipLength;
            clipSamples = new float[sampleCount];
            Clip.GetData(clipSamples, 0);
            List<ChannelPrototype> targetChannels = new List<ChannelPrototype>();
            foreach (ChannelPrototype upmixTarget in UpmixTargets)
                targetChannels.Add(upmixTarget);
            ChannelPrototype[] matrix = ChannelPrototype.StandardMatrix[clipChannels];
            for (int channel = 0; channel < matrix.Length; ++channel)
                if (!targetChannels.Contains(matrix[channel]))
                    targetChannels.Add(matrix[channel]);
            for (int source = 0; source < targetChannels.Count; ++source)
                channels[targetChannels[source]] = new SpatializedChannel(targetChannels[source], this, updateRate);
            mains[0] = GetChannel(ChannelPrototype.FrontLeft);
            mains[1] = GetChannel(ChannelPrototype.FrontRight);
            mains[2] = GetChannel(ChannelPrototype.FrontCenter);
            mains[3] = GetChannel(ChannelPrototype.ScreenLFE);
            mains[4] = GetChannel(ChannelPrototype.RearLeft);
            mains[5] = GetChannel(ChannelPrototype.RearRight);
            mains[6] = GetChannel(ChannelPrototype.SideLeft);
            mains[7] = GetChannel(ChannelPrototype.SideRight);
            GenerateSampleBlock();
        }

        internal SpatializedChannel GetChannel(ChannelPrototype Target) {
            if (channels.ContainsKey(Target))
                return channels[Target];
            return null;
        }

        void GenerateSampleBlock() {
            AudioListener3D listener = AudioListener3D.Current;
            float smoothFactor = 1f - QMath.Lerp(updateRate, listener.SampleRate, (float)Math.Pow(Smoothness, .1f)) / listener.SampleRate * .999f;
            foreach (KeyValuePair<ChannelPrototype, SpatializedChannel> channel in channels)
                Array.Clear(channel.Value.Output, 0, updateRate);
            if (timeSamples >= clipLength) {
                if (Loop)
                    timeSamples %= clipLength;
                else {
                    timeSamples = 0;
                    IsPlaying = false;
                    return;
                }
            }
            if (IsPlaying) {
                foreach (KeyValuePair<ChannelPrototype, SpatializedChannel> Channel in channels)
                    Channel.Value.WrittenOutput = false;
                // Load input channels
                int remaining = clipLength - timeSamples;
                if (remaining > updateRate)
                    remaining = updateRate;
                for (int channel = 0; channel < clipChannels; ++channel) {
                    SpatializedChannel outputChannel = GetChannel(ChannelPrototype.StandardMatrix[clipChannels][channel]);
                    float[] target = outputChannel.Output;
                    for (int offset = 0, srcOffset = timeSamples * clipChannels + channel; offset < remaining; ++offset, srcOffset += clipChannels)
                        target[offset] = clipSamples[srcOffset] * Volume;
                    outputChannel.WrittenOutput = true;
                }
                if (MatrixUpmix) { // Create missing channels via matrix
                    if (mains[0].WrittenOutput && mains[1].WrittenOutput) { // Left and right channels available
                        if (!mains[2].WrittenOutput) { // Create discrete middle channel
                            float[] left = mains[0].Output, right = mains[1].Output, center = mains[2].Output;
                            for (int offset = 0; offset < updateRate; ++offset)
                                center[offset] = (left[offset] + right[offset]) * .5f;
                            mains[2].WrittenOutput = true;
                        }
                        if (!mains[6].WrittenOutput) { // Matrix mix for sides
                            float[] leftFront = mains[0].Output, rightFront = mains[1].Output, leftSide = mains[6].Output, rightSide = mains[7].Output;
                            for (int offset = 0; offset < updateRate; ++offset) {
                                leftSide[offset] = (leftFront[offset] - rightFront[offset]) * .5f;
                                rightSide[offset] = -leftSide[offset];
                            }
                            mains[6].WrittenOutput = mains[7].WrittenOutput = true;
                        }
                        if (!mains[4].WrittenOutput) { // Extend sides to rears...
                            bool rearsAvailable = false; // ...but only if there are rears
                            for (int channel = 0; channel < Listener.Channels.Length; ++channel) {
                                float currentY = Listener.Channels[channel].Y;
                                if (currentY < -135 || currentY > 135) {
                                    rearsAvailable = true;
                                    break;
                                }
                            }
                            if (rearsAvailable) {
                                float[] leftSide = mains[6].Output, rightSide = mains[7].Output, leftRear = mains[4].Output, rightRear = mains[5].Output;
                                for (int offset = 0; offset < updateRate; ++offset) {
                                    leftRear[offset] = (leftSide[offset] *= .5f);
                                    rightRear[offset] = (rightSide[offset] *= .5f);
                                }
                                mains[4].WrittenOutput = mains[5].WrittenOutput = true;
                            }
                        }
                    }
                }
            }
            // Overwrite channel data with new output, even if it's empty
            foreach (KeyValuePair<ChannelPrototype, SpatializedChannel> channel in channels)
                channel.Value.Tick(Effect, smoothFactor, GroundCrossover, Visualize);
            timeSamples += updateRate;
        }

        internal float[][] Tick(SpatializedChannel source, bool groundLevel) {
            if (source.TicksTook == 2) { // Both moving and ground source was fed
                GenerateSampleBlock();
                foreach (KeyValuePair<ChannelPrototype, SpatializedChannel> channel in channels)
                    channel.Value.TicksTook = 0;
            }
            ++source.TicksTook;
            return new float[1][] { source.GetOutput(groundLevel) };
        }

        void OnDestroy() {
            foreach (KeyValuePair<ChannelPrototype, SpatializedChannel> channel in channels)
                channel.Value.Destroy();
            AudioListener3D listener = AudioListener3D.Current;
            listener.SampleRate = oldSampleRate;
            listener.UpdateRate = oldUpdateRate;
        }
    }
}