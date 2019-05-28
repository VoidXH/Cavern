using System;
using System.Collections.Generic;
using UnityEngine;

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

        /// <summary>Playback position in seconds.</summary>
        public float time {
            get => timeSamples / (float)AudioListener3D.Current.SampleRate;
            set => timeSamples = (int)(value * AudioListener3D.Current.SampleRate);
        }

        /// <summary>Playback position in samples.</summary>
        public int timeSamples;

        /// <summary>This height value indicates if a channel is skipped in height processing.</summary>
        internal const float UnsetHeight = -2;

        /// <summary>Imported audio data.</summary>
        float[] ClipSamples;

        /// <summary><see cref="AudioListener3D.UpdateRate"/> for conversion.</summary>
        int UpdateRate;
        /// <summary>Cached <see cref="AudioListener3D.SampleRate"/> as the listener is reconfigured for the Cavernize process.</summary>
        int OldSampleRate;
        /// <summary>Cached <see cref="AudioListener3D.UpdateRate"/> as the listener is reconfigured for the Cavernize process.</summary>
        int OldUpdateRate;

        // TODO: 9-16 CH WAV import

        internal Dictionary<CavernizeChannel, SpatializedChannel> Channels = new Dictionary<CavernizeChannel, SpatializedChannel>();

        void Start() {
            AudioListener3D Listener = AudioListener3D.Current;
            OldSampleRate = Listener.SampleRate;
            OldUpdateRate = Listener.UpdateRate;
            Listener.SampleRate = Clip.frequency;
            UpdateRate = Listener.UpdateRate = Clip.frequency / UpdatesPerSecond;
            int ClipChannels = Clip.channels;
            ClipSamples = new float[ClipChannels * UpdateRate];
            List<CavernizeChannel> TargetChannels = new List<CavernizeChannel>();
            foreach (CavernizeChannel UpmixTarget in CavernizeChannel.UpmixTargets)
                TargetChannels.Add(UpmixTarget);
            CavernizeChannel[] Matrix = CavernizeChannel.StandardMatrix[ClipChannels];
            for (int Channel = 0, ChannelCount = Matrix.Length; Channel < ChannelCount; ++Channel)
                if (!TargetChannels.Contains(Matrix[Channel]))
                    TargetChannels.Add(Matrix[Channel]);
            for (int Source = 0, Sources = TargetChannels.Count; Source < Sources; ++Source)
                Channels[TargetChannels[Source]] = new SpatializedChannel(TargetChannels[Source], this, UpdateRate);
            Mains[0] = GetChannel(CavernizeChannel.FrontLeft);
            Mains[1] = GetChannel(CavernizeChannel.FrontRight);
            Mains[2] = GetChannel(CavernizeChannel.FrontCenter);
            Mains[3] = GetChannel(CavernizeChannel.ScreenLFE);
            Mains[4] = GetChannel(CavernizeChannel.RearLeft);
            Mains[5] = GetChannel(CavernizeChannel.RearRight);
            Mains[6] = GetChannel(CavernizeChannel.SideLeft);
            Mains[7] = GetChannel(CavernizeChannel.SideRight);
            GenerateSampleBlock();
        }

        internal SpatializedChannel GetChannel(CavernizeChannel Target) {
            if (Channels.ContainsKey(Target))
                return Channels[Target];
            return null;
        }

        /// <summary>The channels for a base 7.1 layout.</summary>
        internal readonly SpatializedChannel[] Mains = new SpatializedChannel[8];

        void GenerateSampleBlock() {
            AudioListener3D Listener = AudioListener3D.Current;
            float SmoothFactor = 1f - CavernUtilities.FastLerp(UpdateRate, Listener.SampleRate, (float)Math.Pow(Smoothness, .1f)) / Listener.SampleRate * .999f;
            foreach (KeyValuePair<CavernizeChannel, SpatializedChannel> Channel in Channels)
                Array.Clear(Channel.Value.Output, 0, UpdateRate);
            int MaxLength = Clip.samples;
            if (timeSamples >= MaxLength) {
                if (Loop)
                    timeSamples %= MaxLength;
                else {
                    timeSamples = 0;
                    IsPlaying = false;
                    return;
                }
            }
            int ClipChannels = Clip.channels;
            if (IsPlaying) {
                foreach (KeyValuePair<CavernizeChannel, SpatializedChannel> Channel in Channels)
                    Channel.Value.WrittenOutput = false;
                // Load input channels
                Clip.GetData(ClipSamples, timeSamples);
                for (int Channel = 0; Channel < ClipChannels; ++Channel) {
                    SpatializedChannel OutputChannel = GetChannel(CavernizeChannel.StandardMatrix[ClipChannels][Channel]);
                    float[] Target = OutputChannel.Output;
                    for (int Offset = 0, SrcOffset = Channel; Offset < UpdateRate; ++Offset, SrcOffset += ClipChannels)
                        Target[Offset] = ClipSamples[SrcOffset] * Volume;
                    OutputChannel.WrittenOutput = true;
                }
                if (MatrixUpmix) { // Create missing channels via matrix
                    if (Mains[0].WrittenOutput && Mains[1].WrittenOutput) { // Left and right channels available
                        if (!Mains[2].WrittenOutput) { // Create discrete middle channel
                            float[] Left = Mains[0].Output, Right = Mains[1].Output, Center = Mains[2].Output;
                            for (int Offset = 0; Offset < UpdateRate; ++Offset)
                                Center[Offset] = (Left[Offset] + Right[Offset]) * .5f;
                            Mains[2].WrittenOutput = true;
                        }
                        if (!Mains[6].WrittenOutput) { // Matrix mix for sides
                            float[] LeftFront = Mains[0].Output, RightFront = Mains[1].Output, LeftSide = Mains[6].Output, RightSide = Mains[7].Output;
                            for (int Offset = 0; Offset < UpdateRate; ++Offset) {
                                LeftSide[Offset] = (LeftFront[Offset] - RightFront[Offset]) * .5f;
                                RightSide[Offset] = -LeftSide[Offset];
                            }
                            Mains[6].WrittenOutput = Mains[7].WrittenOutput = true;
                        }
                        if (!Mains[4].WrittenOutput) { // Extend sides to rears...
                            bool RearsAvailable = false; // ...but only if there are rears
                            for (int Channel = 0; Channel < AudioListener3D.ChannelCount; ++Channel) {
                                float CurrentY = AudioListener3D.Channels[Channel].y;
                                if (CurrentY < -135 || CurrentY > 135) {
                                    RearsAvailable = true;
                                    break;
                                }
                            }
                            if (RearsAvailable) {
                                float[] LeftSide = Mains[6].Output, RightSide = Mains[7].Output, LeftRear = Mains[4].Output, RightRear = Mains[5].Output;
                                for (int Offset = 0; Offset < UpdateRate; ++Offset) {
                                    LeftRear[Offset] = (LeftSide[Offset] *= .5f);
                                    RightRear[Offset] = (RightSide[Offset] *= .5f);
                                }
                                Mains[4].WrittenOutput = Mains[5].WrittenOutput = true;
                            }
                        }
                    }
                }
            }
            // Overwrite channel data with new output, even if it's empty
            float EffectMult = Effect * 15f;
            foreach (KeyValuePair<CavernizeChannel, SpatializedChannel> Channel in Channels)
                Channel.Value.Tick(EffectMult, SmoothFactor, GroundCrossover, Visualize);
            if (CenterStays) {
                SpatializedChannel Channel = GetChannel(CavernizeChannel.FrontCenter);
                Channel.Height = UnsetHeight;
                Channel.MovingSource.transform.localPosition = new Vector3(0, 0, 10);
            }
            timeSamples += UpdateRate;
        }

        internal float[] Tick(SpatializedChannel Source, bool GroundLevel) {
            if (Source.TicksTook == 2) { // Both moving and ground source was fed
                GenerateSampleBlock();
                foreach (KeyValuePair<CavernizeChannel, SpatializedChannel> Channel in Channels)
                    Channel.Value.TicksTook = 0;
            }
            ++Source.TicksTook;
            return GroundLevel ? Source.Filter.LowOutput : Source.Filter.HighOutput;
        }

        void OnDestroy() {
            foreach (KeyValuePair<CavernizeChannel, SpatializedChannel> Channel in Channels)
                Channel.Value.Destroy();
            AudioListener3D Listener = AudioListener3D.Current;
            Listener.SampleRate = OldSampleRate;
            Listener.UpdateRate = OldUpdateRate;
        }
    }
}