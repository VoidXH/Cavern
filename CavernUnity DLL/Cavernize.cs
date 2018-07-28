using System;
using UnityEngine;

using Cavern.Helpers;

namespace Cavern {
    /// <summary>Adds height to each channel of a regular surround mix.</summary>
    [AddComponentMenu("Audio/3D Conversion")]
    public class Cavernize : MonoBehaviour {
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
        public int UpdatesPerSecond = 500;
        /// <summary>Delay in samples.</summary>
        [Tooltip("Delay in samples.")]
        public int InitialDelay = 2048;
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
        [Tooltip("Don't spatialize the front channel. This can fix the speech from above anomaly if it's present.")]
        public bool CenterStays = true;

        /// <summary>Manually ask for one update period.</summary>
        [Header("Debug")]
        [Tooltip("Manually ask for one update period.")]
        public bool Manual = false;
        /// <summary>Show converted objects.</summary>
        [Tooltip("Show converted objects.")]
        public bool Visualize = false;

        /// <summary>Playback position in seconds.</summary>
        public float time {
            get { return Now / (float)AudioListener3D.Current.SampleRate; }
            set { Now = LastTime = (int)(value * AudioListener3D.Current.SampleRate); }
        }

        /// <summary>Playback position in samples.</summary>
        public int timeSamples {
            get { return Now; }
            set { Now = LastTime = value; }
        }

        /// <summary>Sources representing imported or created channels.</summary>
        AudioSource3D[] SphericalPoints = new AudioSource3D[CavernChannels];

        /// <summary>Indicates if the previous update was initiated manually.</summary>
        bool PrevManual = false;

        /// <summary>Imported audio data.</summary>
        float[] ClipSamples;
        /// <summary>Last low-passed sample of each channel.</summary>
        float[] LastLow = new float[CavernChannels];
        /// <summary>Last sample of each channel.</summary>
        float[] LastNormal = new float[CavernChannels];
        /// <summary>Last high-passed sample of each channel.</summary>
        float[] LastHigh = new float[CavernChannels];
        /// <summary>Last output for each channel.</summary>
        float[][] Output = new float[CavernChannels][];

        /// <summary>Objects representing imported or created channels.</summary>
        GameObject[] SphericalObjects = new GameObject[CavernChannels];

        /// <summary>Output timer.</summary>
        int Now = 0;
        /// <summary>Output timer in the last frame.</summary>
        int LastTime;
        /// <summary><see cref="Clip"/>'s read position.</summary>
        int ClipLastTime;
        /// <summary>Output timing of Unity.</summary>
        int LastOutputPos = 0;
        /// <summary><see cref="AudioListener3D.UpdateRate"/> for conversion.</summary>
        int UpdateRate;
        /// <summary>Cached <see cref="AudioListener3D.SampleRate"/> as the listener is reconfigured for the Cavernize process.</summary>
        int OldSampleRate;
        /// <summary>Cached <see cref="AudioListener3D.UpdateRate"/> as the listener is reconfigured for the Cavernize process.</summary>
        int OldUpdateRate;
        /// <summary>Cached <see cref="AudioListener3D.MaximumSources"/> as the source limit might be too small for the Cavernize process.</summary>
        int OldMaxSources;

        /// <summary>Visualization renderer for each imported or created channel.</summary>
        Renderer[] SphericalRenderers = new Renderer[CavernChannels];

        /// <summary>Named channel structure.</summary>
        struct CavernizeChannel {
            /// <summary>Y axis angle.</summary>
            public float Y;
            /// <summary>X axis angle.</summary>
            public float X;
            /// <summary>Channel name.</summary>
            public string Name;
            /// <summary>True if the channel is used for Low Frequency Effects.</summary>
            public bool LFE;
            /// <summary>Mute status.</summary>
            public bool Muted;
            /// <summary>Possible upmix target, always gets created.</summary>
            public bool UpmixTarget;

            /// <summary>
            /// Standard channel constructor.
            /// </summary>
            /// <param name="y">Y axis angle</param>
            /// <param name="name">Channel name</param>
            /// <param name="lfe">True if the channel is used for Low Frequency Effects</param>
            /// <param name="muted">Mute status</param>
            /// <param name="upmixTarget">Possible upmix target, always gets created</param>
            public CavernizeChannel(float y, string name, bool lfe = false, bool muted = false, bool upmixTarget = false) {
                Y = y;
                X = 0;
                Name = name;
                LFE = lfe;
                Muted = muted;
                UpmixTarget = upmixTarget;
            }

            /// <summary>Spatial channel constructor.</summary>
            /// <param name="y">Y axis angle</param>
            /// <param name="x">X axis angle</param>
            /// <param name="name">Channel name</param>
            public CavernizeChannel(float y, float x, string name) {
                Y = y;
                X = x;
                Name = name;
                LFE = Muted = UpmixTarget = false;
            }
        }

        // TODO: 9-16 CH WAV import

        /// <summary>Possible channels to use in layouts</summary>
        static readonly CavernizeChannel[] StandardChannels = new CavernizeChannel[] {
            new CavernizeChannel(-30, "Front left"),                     // 00 - L
            new CavernizeChannel(30, "Front right"),                     // 01 - R
            new CavernizeChannel(0, "Front center", false, false, true), // 02 - C
            new CavernizeChannel(0, "LFE", true),                        // 03 - LFE
            new CavernizeChannel(-110, "Side left", false, false, true), // 04 - SL
            new CavernizeChannel(110, "Side right", false, false, true), // 05 - SR
            new CavernizeChannel(-150, "Rear left", false, false, true), // 06 - RL
            new CavernizeChannel(150, "Rear right", false, false, true), // 07 - RR
            new CavernizeChannel(180, "Rear center"),                    // 08 - RC
            //new CavernizeChannel(-15, "Front left center"),              // 09 - LC
            //new CavernizeChannel(15, "Front right center"),              // 10 - RC
            //new CavernizeChannel(0, "Hearing impaired", false, true),    // 11 - HI (muted by default)
            //new CavernizeChannel(0, "Visually impaired narrative", false, true), // 12 - VI (muted by default)
            //new CavernizeChannel(0, "Unused", false, true),              // 13 - UU (muted by default)
            //new CavernizeChannel(0, "Motion data sync", false, true),    // 14 - MD (muted by default)
            //new CavernizeChannel(0, "External sync signal", false, true), // 15 - ES (muted by default)
            //new CavernizeChannel(-70, -45, "Top front left"),            // 16 - TFL
            //new CavernizeChannel(70, -45, "Top front right"),            // 17 - TFR
            //new CavernizeChannel(-130, -45, "Top side left"),            // 18 - TSL
            //new CavernizeChannel(130, -45, "Top side right"),            // 19 - TSR
            //new CavernizeChannel(0, "Sign language video", false, true), // 20 - SL (muted by default)
            //new CavernizeChannel(0, 90, "Bottom surround"),              // 21 - BS
            //new CavernizeChannel(0, -45, "Top front center"),            // 22 - TFC
            //new CavernizeChannel(0, -90, "God's voice"),                 // 23 - GV
        };

        /// <summary>Maximum possible generated channel count.</summary>
        static readonly int CavernChannels = StandardChannels.Length;
        /// <summary>True for each source channel if it was processed the last update.</summary>
        [NonSerialized]
        public bool[] WrittenOutput = new bool[CavernChannels];
        /// <summary>The height of each source channel in the range of -0.2 to 1.</summary>
        [NonSerialized]
        public float[] ChannelHeights = new float[CavernChannels];

        /// <summary>Default channel orders for each input channel count.</summary>
        static int[][] ChannelMatrix = {
            new int[0],
            new int[]{2}, // 1CH: 1.0 (C)
            new int[]{0, 1}, // 2CH: 2.0 (L, R)
            new int[]{0, 1, 2}, // 3CH: 3.0 (L, R, C) - non-standard
            new int[]{0, 1, 4, 5}, // 4CH: 4.0 (L, R, SL, SR)
            new int[]{0, 1, 2, 4, 5}, // 5CH: 5.0 (L, R, C, SL, SR)
            new int[]{0, 1, 2, 3, 4, 5}, // 6CH: 5.1 (L, R, C, LFE, SL, SR)
            new int[]{0, 1, 2, 3, 4, 5, 8}, // 7CH: 6.1 (L, R, C, LFE, SL, SR, RC)
            new int[]{0, 1, 2, 3, 6, 7, 4, 5}, // 8CH: 7.1 (L, R, C, LFE, RL, RR, SL, SR)
            // These are DCP orders, with messy standardization, and are unused in commercial applications. Revision is recommended for Cavernizing non-5.1 DCPs.
            //new int[]{0, 1, 2, 3, 6, 7, 4, 5, 8}, // 9CH: 8.1 (L, R, C, LFE, RL, RR, SL, SR, RC) - non-standard
            //new int[]{0, 1, 2, 3, 6, 7, 4, 5, 16, 17}, // 10CH: 7.1.2 (out-of-order Cavern DCP) (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR)
            //new int[]{0, 1, 2, 3, 6, 7, 4, 5, 16, 17. 21}, // 11CH: 7.1.2.1 (out-of-order Cavern XL DCP) (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR, BS)
            //new int[]{0, 1, 2, 3, 4, 5, 16, 17, 22, 23, 18, 19}, // 12CH: Barco Auro 11.1 (L, R, C, LFE, SL, SR, TFL, TFR, TFC, GV, TSL, TSR)
            //new int[]{0, 1, 2, 22, 6, 7, 4, 5, 16, 17, 18, 19, 4}, // 13CH: 12-Track (L, R, C, TFC, RL, RR, SL, SR, TFL, TFR, TSL, TSR, LFE)
            //new int[]{0, 1, 2, 3, 6, 7, 4, 5, 16, 17, 22, 23, 18, 19}, // 14CH: Barco Auro 13.1 (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR, TFC, GV, TSL, TSR)
            //new int[]{0, 1, 2, 3, 4, 5, 16, 17, 9, 10, 6, 7, 14, 15, 20}, // 15CH: Cavern (L, R, C, LFE, SL, SR, HI, VI, TL, TR, MD, RR, ES, SL) - non-standard
            //new int[]{0, 1, 2, 3, 4, 5, 16, 17, 9, 10, 6, 7, 14, 15, 20, 21}, // 16CH: Cavern XL (L, R, C, LFE, SL, SR, TL, TR, UU, UU, RL, RR, MD, ES, SL, BS) - non-st
        };

        void Start() {
            AudioListener3D Listener = AudioListener3D.Current;
            OldSampleRate = Listener.SampleRate;
            OldUpdateRate = Listener.UpdateRate;
            Listener.SampleRate = Clip.frequency;
            UpdateRate = Listener.UpdateRate = Clip.frequency / UpdatesPerSecond;
            ClipLastTime = InitialDelay;
            LastTime = Now;
            OldMaxSources = AudioListener3D.MaximumSources;
            if (AudioListener3D.MaximumSources < CavernChannels)
                AudioListener3D.MaximumSources = CavernChannels;
            int ClipChannels = Clip.channels;
            ClipSamples = new float[ClipChannels * UpdateRate];
            for (int Source = 0; Source < CavernChannels; ++Source) {
                bool Spawn = StandardChannels[Source].UpmixTarget; // Only spawn the channel if it is used or could be used
                for (int Channel = 0; Channel < ClipChannels; ++Channel)
                    Spawn |= ChannelMatrix[ClipChannels][Channel] == Source;
                if (Spawn) {
                    if (Source != 3)
                        SphericalObjects[Source] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    else
                        SphericalObjects[Source] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    SphericalRenderers[Source] = SphericalObjects[Source].GetComponent<Renderer>();
                    SphericalObjects[Source].name = StandardChannels[Source].Name;
                    AudioSource3D NewSource = SphericalPoints[Source] = SphericalObjects[Source].AddComponent<AudioSource3D>();
                    NewSource.Clip = AudioClip.Create(string.Empty, Listener.SampleRate, 1, Listener.SampleRate, false);
                    NewSource.Loop = true;
                    NewSource.VolumeRolloff = Rolloffs.Disabled;
                    NewSource.LFE = StandardChannels[Source].LFE;
                    SphericalObjects[Source].AddComponent<ScaleByGain>().Source = NewSource;
                    if (StandardChannels[Source].Muted)
                        NewSource.Volume = 0;
                    SphericalObjects[Source].transform.position =
                        CavernUtilities.VectorScale(CavernUtilities.PlaceInCube(new Vector3(0, StandardChannels[Source].Y)), AudioListener3D.EnvironmentSize);
                }
            }
            for (int Channel = 0; Channel < CavernChannels; ++Channel)
                Output[Channel] = new float[UpdateRate];
        }

        /// <summary>Runs the frame update function.</summary>
        public void ForcedUpdate() {
            Update();
        }

        unsafe void Update() {
            // Reset
            for (int Channel = 0; Channel < CavernChannels; ++Channel)
                Array.Clear(Output[Channel], 0, UpdateRate);
            // Precalculations
            AudioListener3D Listener = AudioListener3D.Current;
            float SmoothFactor = 1f - CavernUtilities.FastLerp(UpdateRate, Listener.SampleRate, (float)Math.Pow(Smoothness, .1f)) / Listener.SampleRate * .999f;
            // Timing
            int Increase = SphericalPoints[2].timeSamples < LastOutputPos ? SphericalPoints[2].timeSamples + Listener.SampleRate - LastOutputPos :
                SphericalPoints[2].timeSamples - LastOutputPos;
            Now += Increase;
            LastOutputPos = SphericalPoints[2].timeSamples;
            if (Manual)
                Now = LastTime + UpdateRate;
            else if (!IsPlaying) {
                Now = LastTime;
                if (!PrevManual)
                    ClipLastTime += Increase;
                for (int Channel = 0; Channel < CavernChannels; ++Channel) {
                    if (SphericalObjects[Channel]) {
                        SphericalPoints[Channel].Mute = true;
                        SphericalPoints[Channel].Clip.SetData(Output[Channel], ClipLastTime % Listener.SampleRate);
                    }
                }
            }
            bool Mute = !(IsPlaying || PrevManual || Manual);
            PrevManual = Manual;
            while (LastTime < Now) {
                // Reset
                int From = LastTime;
                int MaxLength = Clip.samples;
                if (From >= MaxLength) {
                    if (Loop) {
                        Now %= MaxLength;
                        LastTime %= Clip.samples;
                        From %= Clip.samples;
                    } else {
                        Now = 0;
                        LastTime = 0;
                        IsPlaying = false;
                        return;
                    }
                }
                WrittenOutput = new bool[CavernChannels];
                int Channels = Clip.channels;
                LastTime += UpdateRate;
                int ClipFrom = ClipLastTime;
                ClipLastTime += UpdateRate;
                if (IsPlaying || Manual) {
                    // Load input channels
                    Clip.GetData(ClipSamples, From);
                    for (int Channel = 0; Channel < Channels; ++Channel) {
                        int OutputChannel = ChannelMatrix[Channels][Channel];
                        fixed (float* Target = Output[OutputChannel], Source = ClipSamples)
                            for (int Offset = 0, SrcOffset = Channel; Offset < UpdateRate; ++Offset, SrcOffset += Channels)
                                Target[Offset] = Source[SrcOffset] * Volume;
                        WrittenOutput[OutputChannel] = true;
                    }
                    if (MatrixUpmix) { // Create missing channels via matrix
                        if (WrittenOutput[0] && WrittenOutput[1]) { // Left and right channels available
                            if (!WrittenOutput[2]) { // Create discrete middle channel
                                fixed (float* Left = Output[0], Right = Output[1], Center = Output[2])
                                    for (int Offset = 0; Offset < UpdateRate; ++Offset)
                                        Center[Offset] = (Left[Offset] + Right[Offset]) * .5f;
                                WrittenOutput[2] = true;
                            }
                            if (!WrittenOutput[4]) { // Matrix mix for sides
                                fixed (float* LeftFront = Output[0], RightFront = Output[1], LeftSide = Output[4], RightSide = Output[5]) {
                                    for (int Offset = 0; Offset < UpdateRate; ++Offset) {
                                        LeftSide[Offset] = (LeftFront[Offset] - RightFront[Offset]) * .5f;
                                        RightSide[Offset] = -LeftSide[Offset];
                                    }
                                }
                                WrittenOutput[4] = WrittenOutput[5] = true;
                            }
                            if (!WrittenOutput[6]) { // Extend sides to rears
                                bool RearsAvailable = false; // ...but only if there are rears
                                for (int Channel = 0; Channel < AudioListener3D.ChannelCount; ++Channel) {
                                    float CurrentY = AudioListener3D.Channels[Channel].y;
                                    if (CurrentY < -135 || CurrentY > 135) {
                                        RearsAvailable = true;
                                        break;
                                    }
                                }
                                if (RearsAvailable) {
                                    fixed (float* LeftSide = Output[4], RightSide = Output[5], LeftRear = Output[6], RightRear = Output[7]) {
                                        for (int Offset = 0; Offset < UpdateRate; ++Offset) {
                                            LeftRear[Offset] = (LeftSide[Offset] *= .5f);
                                            RightRear[Offset] = (RightSide[Offset] *= .5f);
                                        }
                                    }
                                    WrittenOutput[6] = WrittenOutput[7] = true;
                                }
                            }
                        }
                    }
                }
                // Write output
                float EffectMult = Effect * 15f;
                for (int Channel = 0; Channel < CavernChannels; ++Channel) {
                    if (SphericalObjects[Channel]) {
                        SphericalPoints[Channel].Mute = Mute;
                        SphericalPoints[Channel].Clip.SetData(Output[Channel], ClipFrom % Listener.SampleRate); // Overwrite channel data with new output, even if it's empty
                        SphericalRenderers[Channel].enabled = Visualize && WrittenOutput[Channel];
                        if (WrittenOutput[Channel]) { // Create height for channels with new audio data
                            float MaxDepth = .0001f, MaxHeight = .0001f;
                            int SamplesToProcess = UpdateRate;
                            fixed (float* Sample = Output[Channel]) {
                                for (int Offset = 0; Offset < SamplesToProcess; ++Offset) {
                                    // Height is generated by a simplified measurement of volume and pitch
                                    LastHigh[Channel] = .9f * (LastHigh[Channel] + Sample[Offset] - LastNormal[Channel]);
                                    float AbsHigh = CavernUtilities.Abs(LastHigh[Channel]);
                                    if (MaxHeight < AbsHigh)
                                        MaxHeight = AbsHigh;
                                    LastLow[Channel] = LastLow[Channel] * .99f + LastHigh[Channel] * .01f;
                                    float AbsLow = CavernUtilities.Abs(LastLow[Channel]);
                                    if (MaxDepth < AbsLow)
                                        MaxDepth = AbsLow;
                                    LastNormal[Channel] = Sample[Offset];
                                }
                            }
                            MaxHeight = (MaxHeight - MaxDepth * 1.2f) * EffectMult;
                            if (MaxHeight < -.2f)
                                MaxHeight = -.2f;
                            else if (MaxHeight > 1)
                                MaxHeight = 1;
                            ChannelHeights[Channel] = CavernUtilities.FastLerp(ChannelHeights[Channel], MaxHeight, SmoothFactor);
                            Transform TargetTransform = SphericalObjects[Channel].transform;
                            TargetTransform.position = CavernUtilities.FastLerp(TargetTransform.position,
                                new Vector3(TargetTransform.position.x, MaxHeight * AudioListener3D.EnvironmentSize.y, TargetTransform.position.z), SmoothFactor);
                        }
                    }
                }
                if (CenterStays) {
                    ChannelHeights[2] = -2;
                    SphericalObjects[2].transform.position = new Vector3(0, 0, 10);
                }
            }
            Manual = false;
        }

        void OnDestroy() {
            for (int Source = 0; Source < CavernChannels; ++Source) {
                if (SphericalObjects[Source]) {
                    Destroy(SphericalPoints[Source].Clip);
                    Destroy(SphericalObjects[Source]);
                }
            }
            AudioListener3D Listener = AudioListener3D.Current;
            Listener.SampleRate = OldSampleRate;
            Listener.UpdateRate = OldUpdateRate;
            AudioListener3D.MaximumSources = OldMaxSources;
        }
    }
}