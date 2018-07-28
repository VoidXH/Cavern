using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern {
    [AddComponentMenu("Audio/3D Audio Source")]
    public partial class AudioSource3D : MonoBehaviour {
        // ------------------------------------------------------------------
        // Constants
        // ------------------------------------------------------------------
        const float PiRecip = .3183098861837906f;
        const float SpeedOfSound = 340.29f;

        // ------------------------------------------------------------------
        // Internal helpers
        // ------------------------------------------------------------------
        /// <summary>Is the user's speaker layout symmetrical?</summary>
        internal static bool Symmetric = false;

        // ------------------------------------------------------------------
        // Lifecycle helpers
        // ------------------------------------------------------------------
        void OnEnable() {
            if (RandomPosition)
                timeSamples = UnityEngine.Random.Range(0, Clip.samples);
            Distance = GetDistance(transform.position);
            SetRolloff();
            Node = AudioListener3D.ActiveSources.AddLast(this);
        }

        void OnDisable() {
            Node.List.Remove(Node);
        }

        // ------------------------------------------------------------------
        // Private vars
        // ------------------------------------------------------------------
        /// <summary>Indicator of cached echo settings.</summary>
        bool CachedEcho = false;
        /// <summary>The collection should be performed, as all requirements are met.</summary>
        bool Collectible;

        /// <summary><see cref="PitchedUpdateRate"/> without resampling.</summary>
        int BaseUpdateRate;
        /// <summary>Cached channel count of <see cref="Clip"/>.</summary>
        int ClipChannels;
        /// <summary>Cached length of <see cref="Clip"/>.</summary>
        int ClipSamples;
        /// <summary>Samples required to match the listener's update rate after pitch changes.</summary>
        int PitchedUpdateRate;

        /// <summary>Actually used pitch multiplier including the Doppler effect.</summary>
        float CalculatedPitch;
        /// <summary>Distance from the listener.</summary>
        float Distance;
        /// <summary><see cref="Distance"/> in the previous frame, required for Doppler effect calculation.</summary>
        float LastDistance;
        /// <summary>The last sample past the filter is required for lowpass effects.</summary>
        float LastLowpassedSample = 0;
        /// <summary>Cached <see cref="EchoVolume"/> after <see cref="AudioListener3D.HeadphoneVirtualizer"/> was set.</summary>
        float OldEchoVolume;
        /// <summary>Cached <see cref="EchoDelay"/> after <see cref="AudioListener3D.HeadphoneVirtualizer"/> was set.</summary>
        float OldEchoDelay;
        /// <summary>Sample rate multiplier to match the system sample rate.</summary>
        float ResampleMult;

        /// <summary>Past output samples for echo effect.</summary>
        float[] EchoBuffer = new float[0];
        /// <summary>Sample buffer from the clip.</summary>
        float[] OriginalSamples;

        /// <summary>Remaining delay until starting playback.</summary>
        ulong Delay = 0;

        /// <summary>Linked list access for the sources' list.</summary>
        LinkedListNode<AudioSource3D> Node;

        /// <summary>Last source position required for smoothing movement.</summary>
        Vector3 LastPosition;

        /// <summary>Gets the distance of the <see cref="AudioListener3D"/> from the given position.</summary>
        /// <param name="From">World target</param>
        /// <returns>Distance of the listener and the given point</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetDistance(Vector3 From) {
            Vector3 ListenerPos = AudioListener3D.LastPosition;
            float xDist = From.x - ListenerPos.x, yDist = From.y - ListenerPos.y, zDist = From.z - ListenerPos.z;
            return Mathf.Sqrt(xDist * xDist + yDist * yDist + zDist * zDist);
        }

        /// <summary>Resamples a single channel.</summary>
        /// <param name="Samples">Samples of the source channel</param>
        /// <param name="From">Old sample rate</param>
        /// <param name="To">New sample rate</param>
        /// <returns>Returns a resampled version of the given array</returns>
        internal static float[] Resample(float[] Samples, int From, int To) {
            if (From == To)
                return Samples;
            float[] Output = new float[To];
            float Ratio = From / (float)To;
            if (AudioListener3D.Current.AudioQuality < QualityModes.High) { // Nearest neighbour below High
                for (int i = 0; i < To; ++i)
                    Output[i] = Samples[(int)(i * Ratio)];
            } else if (AudioListener3D.Current.AudioQuality < QualityModes.Perfect) { // Lerp below Perfect
                int End = Samples.Length - 1;
                for (int i = 0; i < To; ++i) {
                    float FromPos = i * Ratio;
                    int Sample = (int)FromPos;
                    if (Sample < End)
                        Output[i] = CavernUtilities.FastLerp(Samples[Sample], Samples[++Sample], FromPos % 1);
                    else
                        Output[i] = Samples[Sample];
                }
            } else { // Catmull-Rom on Perfect
                int Start = Mathf.CeilToInt(1 / Ratio), End = Samples.Length - 3;
                for (int i = 0; i < Start; ++i)
                    Output[i] = Samples[i];
                for (int i = Start; i < To; ++i) {
                    float FromPos = i * Ratio;
                    int Sample = (int)FromPos;
                    if (Sample < End) {
                        float CatRomT = FromPos % 1, CatRomT2 = CatRomT * CatRomT;
                        float P0 = Samples[Sample - 1], P1 = Samples[Sample], P2 = Samples[Sample + 1], P3 = Samples[Sample + 2];
                        Output[i] = ((P1 * 2) + (P2 - P0) * CatRomT + (P0 * 2 - P1 * 5 + P2 * 4 - P3) * CatRomT2 +
                            (3 * P1 - P0 - 3 * P2 + P3) * CatRomT2 * CatRomT) * .5f;
                    } else
                        Output[i] = Samples[Sample];
                }
            }
            return Output;
        }

        /// <summary>Clamp a number between two values.</summary>
        /// <param name="x">Input number</param>
        /// <param name="min">Minimum</param>
        /// <param name="max">Maximum</param>
        /// <returns>X between Minimum and Maximum</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Clamp(float x, float min, float max) {
            if (x < min)
                return min;
            if (x > max)
                return max;
            return x;
        }

        /// <summary>Calculate distance from the <see cref="AudioListener3D"/> and choose the closest sources to play.</summary>
        internal void Precalculate() {
            if (Clip && IsPlaying) {
                LastPosition = transform.position;
                LastDistance = Distance;
                Distance = GetDistance(LastPosition);
                CavernUtilities.BottomlistHandler(AudioListener3D.SourceDistances, AudioListener3D.MaximumSources, Distance);
            }
        }

        /// <summary>Cache the samples if the source should be rendered. This wouldn't be thread safe.</summary>
        internal void Precollect() {
            if (Clip && (Collectible = CavernUtilities.ArrayContains(AudioListener3D.SourceDistances, AudioListener3D.MaximumSources, Distance))) {
                AudioListener3D Listener = AudioListener3D.Current;
                ClipChannels = Clip.channels;
                ClipSamples = Clip.samples;
                if (Listener.AudioQuality != QualityModes.Low) {
                    if (DopplerLevel == 0)
                        CalculatedPitch = Pitch;
                    else
                        CalculatedPitch = Clamp(Pitch * DopplerLevel * SpeedOfSound / (SpeedOfSound - (LastDistance - Distance) /
                            AudioListener3D.PulseDelta), .5f, 3f);
                } else
                    CalculatedPitch = 1; // Disable any pitch change on low quality
                    
                bool NeedsResampling = Listener.SampleRate != Clip.frequency;
                if (NeedsResampling)
                    ResampleMult = (float)Clip.frequency / Listener.SampleRate;
                else
                    ResampleMult = 1;
                BaseUpdateRate = (int)(Listener.UpdateRate * CalculatedPitch);
                PitchedUpdateRate = (int)(BaseUpdateRate * ResampleMult);
                OriginalSamples = new float[ClipChannels * PitchedUpdateRate];
                Clip.GetData(OriginalSamples, timeSamples);
            } else
                OriginalSamples = null;
        }

        /// <summary>Process the source and returns a mix to be added to the output.</summary>
        internal unsafe float[] Collect() {
            if (OriginalSamples == null)
                return null;
            AudioListener3D Listener = AudioListener3D.Current;
            if (Delay > 0) {
                Delay -= (ulong)Listener.UpdateRate;
                return null;
            }
            if (!IsPlaying || !Collectible)
                return null;
            int Channels = AudioListener3D.ChannelCount;
            float[] Rendered = new float[Listener.UpdateRate * Channels];
            bool OutputRawLFE = !Listener.LFESeparation || LFE;
            // Update rate calculation
            int UpdateRate = Listener.UpdateRate;
            int ResampledNow = (int)(timeSamples / ResampleMult);
            if (!Mute) {
                bool Blend2D = SpatialBlend != 1, Blend3D = SpatialBlend != 0;
                bool HighQuality = Listener.AudioQuality >= QualityModes.High;
                bool StereoClip = ClipChannels == 2;
                // Mono mix
                float[] Samples = new float[PitchedUpdateRate];
                if (Blend3D || !StereoClip) {
                    if (ClipChannels == 1)
                        Array.Copy(OriginalSamples, Samples, PitchedUpdateRate);
                    else {
                        if (HighQuality) { // Mono downmix above medium quality
                            fixed (float* SampleArr = Samples, OriginalArr = OriginalSamples) {
                                float* Sample = SampleArr, OrigSamples = OriginalArr;
                                int SamplesToGet = PitchedUpdateRate;
                                while (SamplesToGet-- != 0) {
                                    for (int ChannelPos = 0; ChannelPos < ClipChannels; ++ChannelPos)
                                        *Sample += OrigSamples[ChannelPos];
                                    OrigSamples += ClipChannels;
                                    *Sample++ /= ClipChannels;
                                }
                            }
                        } else { // First channel only otherwise
                            for (int Sample = 0, OrigSample = 0; Sample < PitchedUpdateRate; ++Sample, OrigSample += ClipChannels)
                                Samples[Sample] = OriginalSamples[OrigSample];
                        }
                    }
                }
                if (Blend2D) { // 2D mix
                    float Volume2D = Volume * (1f - SpatialBlend);
                    if (!StereoClip) {
                        int Divisor = 0;
                        for (int Channel = 0; Channel < Channels; ++Channel)
                            if (!AudioListener3D.Channels[Channel].LFE)
                                Divisor++;
                        if (Divisor != 0) {
                            Volume2D /= Divisor;
                            Samples = Resample(Samples, Samples.Length, UpdateRate);
                            int ActualSample = 0;
                            for (int Sample = 0; Sample < UpdateRate; ++Sample) {
                                float GainedSample = Samples[Sample] * Volume2D;
                                for (int Channel = 0; Channel < Channels; ++Channel)
                                    Rendered[ActualSample++] += GainedSample;
                            }
                        }
                    } else {
                        float[] LeftSamples = new float[PitchedUpdateRate], RightSamples = new float[PitchedUpdateRate];
                        int ActualSample = 0;
                        for (int Sample = 0; Sample < PitchedUpdateRate; ++Sample) {
                            LeftSamples[Sample] = OriginalSamples[ActualSample++];
                            RightSamples[Sample] = OriginalSamples[ActualSample++];
                        }
                        LeftSamples = Resample(LeftSamples, LeftSamples.Length, UpdateRate);
                        RightSamples = Resample(RightSamples, RightSamples.Length, UpdateRate);
                        int LeftDivisor = 0, RightDivisor = 0;
                        for (int Channel = 0; Channel < Channels; ++Channel) {
                            Channel CurrentChannel = AudioListener3D.Channels[Channel];
                            if (!CurrentChannel.LFE) {
                                if (CurrentChannel.y < 0)
                                    LeftDivisor += 1;
                                else if (CurrentChannel.y > 0)
                                    RightDivisor += 1;
                            }
                        }
                        float LeftVolume = LeftDivisor == 0 ? 0 : Volume2D / LeftDivisor, RightVolume = RightDivisor == 0 ? 0 : Volume2D / RightDivisor;
                        if (StereoPan < 0)
                            RightVolume *= -StereoPan * StereoPan + 1;
                        else if (StereoPan > 0)
                            LeftVolume *= 1 - StereoPan * StereoPan;
                        float HalfVolume2D = Volume2D * .5f;
                        ActualSample = 0;
                        for (int Sample = 0; Sample < UpdateRate; ++Sample) {
                            float LeftSample = LeftSamples[Sample], RightSample = RightSamples[Sample],
                                LeftGained = LeftSample * LeftVolume, RightGained = RightSample * RightVolume;
                            for (int Channel = 0; Channel < Channels; ++Channel) {
                                if (AudioListener3D.Channels[Channel].LFE) {
                                    if (OutputRawLFE)
                                        Rendered[ActualSample] += (LeftSample + RightSample) * HalfVolume2D;
                                } else if (!LFE) {
                                    if (AudioListener3D.Channels[Channel].y < 0)
                                        Rendered[ActualSample] += LeftGained;
                                    else if (AudioListener3D.Channels[Channel].y > 0)
                                        Rendered[ActualSample] += RightGained;
                                }
                                ++ActualSample;
                            }
                        }
                    }
                }
                if (Blend3D && Distance < Listener.Range) { // 3D mix, if the source is in range
                    Vector3 Direction = AudioListener3D.LastRotationInverse * (LastPosition - AudioListener3D.LastPosition);
                    float RolloffDistance = GetRolloff();
                    Samples = Resample(Samples, Samples.Length, UpdateRate);
                    BaseUpdateRate = Samples.Length;
                    // Distance lowpass, if enabled
                    if (DistanceLowpass != 0) {
                        float DistanceScale = Distance * DistanceLowpass;
                        if (DistanceScale > 1)
                            CavernUtilities.Lowpass(Samples, ref LastLowpassedSample, BaseUpdateRate, 1f - 1f / DistanceScale);
                    }
                    // Buffer for echo, if enabled
                    if (EchoVolume != 0) {
                        int SampleRate = Listener.SampleRate;
                        if (EchoBuffer.Length != SampleRate)
                            EchoBuffer = new float[SampleRate];
                        int EchoBufferPosition = ResampledNow % SampleRate;
                        if (EchoBufferPosition + BaseUpdateRate >= SampleRate) {
                            int FirstRun = SampleRate - EchoBufferPosition;
                            for (int Sample = 0; Sample < FirstRun; ++Sample)
                                EchoBuffer[EchoBufferPosition++] = Samples[Sample];
                            EchoBufferPosition = 0;
                            for (int Sample = FirstRun; Sample < BaseUpdateRate; ++Sample)
                                EchoBuffer[EchoBufferPosition++] = Samples[Sample];
                        } else for (int Sample = 0; Sample < BaseUpdateRate; ++Sample)
                                EchoBuffer[EchoBufferPosition++] = Samples[Sample];
                    }
                    // Echo preparations
                    bool SkipEcho = false;
                    if (Listener.HeadphoneVirtualizer) {
                        float DirectionMagnitudeRecip = 1f / (Direction.magnitude + .0001f);
                        if (!CachedEcho) {
                            OldEchoVolume = EchoVolume;
                            OldEchoDelay = EchoDelay;
                            CachedEcho = true;
                        }
                        SkipEcho = EchoVolume == 0;
                        Vector3 Forward = AudioListener3D.LastRotation * Vector3.forward, Upward = AudioListener3D.LastRotation * Vector3.up;
                        float ForwardScalar = Direction.x * Forward.x + Direction.y * Forward.y + Direction.z * Forward.z,
                            UpwardScalar = Direction.x * Upward.x + Direction.y * Upward.y + Direction.z * Upward.z;
                        EchoVolume = (float)Math.Acos(ForwardScalar / (Forward.magnitude + .0001f) * DirectionMagnitudeRecip) * PiRecip; // Set volume by angle diff
                        float UpwardMatch = (float)Math.Acos(UpwardScalar / (Upward.magnitude + .0001f) * DirectionMagnitudeRecip) * PiRecip;
                        EchoDelay = (48f - 43.2f * UpwardMatch) / Listener.SampleRate; // Delay simulates height difference
                    } else if (CachedEcho) {
                        EchoVolume = OldEchoVolume;
                        EchoDelay = OldEchoDelay;
                        CachedEcho = false;
                    }
                    // ------------------------------------------------------------------
                    // Balance-based engine for symmetrical layouts
                    // ------------------------------------------------------------------
                    if (Symmetric) {
                        float Volume3D = Volume * RolloffDistance * SpatialBlend;
                        if (!LFE) {
                            // Find closest channels by cubical pos
                            int BFL = -1, BFR = -1, BRL = -1, BRR = -1, TFL = -1, TFR = -1, TRL = -1, TRR = -1; // Each direction (bottom/top, front/rear, left/right)
                            float ClosestTop = 1.1f, ClosestBottom = -1.1f, ClosestTF = 1.1f, ClosestTR = -1.1f,
                                ClosestBF = 1.1f, ClosestBR = -1.1f; // Closest layers on y/z
                            Direction.x /= AudioListener3D.EnvironmentSize.x;
                            Direction.y /= AudioListener3D.EnvironmentSize.y;
                            Direction.z /= AudioListener3D.EnvironmentSize.z;
                            for (int Channel = 0; Channel < Channels; ++Channel) { // Find closest horizontal layers
                                if (!AudioListener3D.Channels[Channel].LFE) {
                                    float ChannelY = AudioListener3D.Channels[Channel].CubicalPos.y;
                                    if (ChannelY < Direction.y) {
                                        if (ChannelY > ClosestBottom)
                                            ClosestBottom = ChannelY;
                                    } else if (ChannelY < ClosestTop)
                                        ClosestTop = ChannelY;
                                }
                            }
                            for (int Channel = 0; Channel < Channels; ++Channel) {
                                if (!AudioListener3D.Channels[Channel].LFE) {
                                    Vector3 ChannelPos = AudioListener3D.Channels[Channel].CubicalPos;
                                    if (ChannelPos.y == ClosestBottom) // Bottom layer
                                        AssignHorizontalLayer(Channel, ref BFL, ref BFR, ref BRL, ref BRR, ref ClosestBF, ref ClosestBR, Direction, ChannelPos);
                                    if (ChannelPos.y == ClosestTop) // Top layer
                                        AssignHorizontalLayer(Channel, ref TFL, ref TFR, ref TRL, ref TRR, ref ClosestTF, ref ClosestTR, Direction, ChannelPos);
                                }
                            }
                            FixIncompleteLayer(ref TFL, ref TFR, ref TRL, ref TRR); // Fix incomplete top layer
                            if (BFL == -1 && BFR == -1 && BRL == -1 && BRR == -1) { // Fully incomplete bottom layer, use top
                                BFL = TFL; BFR = TFR; BRL = TRL; BRR = TRR;
                            } else
                                FixIncompleteLayer(ref BFL, ref BFR, ref BRL, ref BRR); // Fix incomplete bottom layer
                            if (TFL == -1 || TFR == -1 || TRL == -1 || TRR == -1) { // Fully incomplete top layer, use bottom
                                TFL = BFL; TFR = BFR; TRL = BRL; TRR = BRR;
                            }
                            // Spatial mix
                            float TopVol, BottomVol;
                            if (TFL != BFL) { // Height ratio calculation
                                float BottomY = AudioListener3D.Channels[BFL].CubicalPos.y;
                                TopVol = (Direction.y - BottomY) / (AudioListener3D.Channels[TFL].CubicalPos.y - BottomY);
                                BottomVol = 1f - TopVol;
                            } else
                                TopVol = BottomVol = .5f;
                            float BFVol = LengthRatio(BRL, BFL, Direction.z), TFVol = LengthRatio(TRL, TFL, Direction.z), // Length ratios
                                BFRVol = WidthRatio(BFL, BFR, Direction.x), BRRVol = WidthRatio(BRL, BRR, Direction.x), // Width ratios
                                TFRVol = WidthRatio(TFL, TFR, Direction.x), TRRVol = WidthRatio(TRL, TRR, Direction.x),
                                InnerVolume3D = Volume3D;
                            if (Size != 0) {
                                BFVol = CavernUtilities.FastLerp(BFVol, .5f, Size);
                                TFVol = CavernUtilities.FastLerp(TFVol, .5f, Size);
                                BFRVol = CavernUtilities.FastLerp(BFRVol, .5f, Size);
                                BRRVol = CavernUtilities.FastLerp(BRRVol, .5f, Size);
                                TFRVol = CavernUtilities.FastLerp(TFRVol, .5f, Size);
                                TRRVol = CavernUtilities.FastLerp(TRRVol, .5f, Size);
                                InnerVolume3D *= 1f - Size;
                                float ExtraChannelVolume = Volume3D * Size / Channels;
                                for (int Channel = 0; Channel < Channels; ++Channel)
                                    UsedOutputFunc(Samples, Rendered, UpdateRate, ExtraChannelVolume, Channel, Channels);
                            }
                            float BRVol = 1f - BFVol, TRVol = 1f - TFVol; // Remaining length ratios
                            BottomVol *= InnerVolume3D; TopVol *= InnerVolume3D; BFVol *= BottomVol; BRVol *= BottomVol; TFVol *= TopVol; TRVol *= TopVol;
                            UsedOutputFunc(Samples, Rendered, UpdateRate, BFVol * (1f - BFRVol), BFL, Channels);
                            UsedOutputFunc(Samples, Rendered, UpdateRate, BFVol * BFRVol, BFR, Channels);
                            UsedOutputFunc(Samples, Rendered, UpdateRate, BRVol * (1f - BRRVol), BRL, Channels);
                            UsedOutputFunc(Samples, Rendered, UpdateRate, BRVol * BRRVol, BRR, Channels);
                            UsedOutputFunc(Samples, Rendered, UpdateRate, TFVol * (1f - TFRVol), TFL, Channels);
                            UsedOutputFunc(Samples, Rendered, UpdateRate, TFVol * TFRVol, TFR, Channels);
                            UsedOutputFunc(Samples, Rendered, UpdateRate, TRVol * (1f - TRRVol), TRL, Channels);
                            UsedOutputFunc(Samples, Rendered, UpdateRate, TRVol * TRRVol, TRR, Channels);
                        }
                        // LFE mix
                        if (OutputRawLFE) {
                            for (int Channel = 0; Channel < Channels; ++Channel)
                                if (AudioListener3D.Channels[Channel].LFE)
                                    UsedOutputFunc(Samples, Rendered, UpdateRate, Volume3D, Channel, Channels);
                        }
                        // Echo
                        if (!SkipEcho && EchoVolume != 0 && !LFE) {
                            Volume3D *= EchoVolume;
                            int EchoStart = (int)((1f - EchoDelay) * Listener.SampleRate) + ResampledNow;
                            int MultichannelUpdateRate = BaseUpdateRate * Channels;
                            for (int Channel = 0; Channel < Channels; ++Channel) {
                                if (!AudioListener3D.Channels[Channel].LFE) {
                                    int EchoPos = EchoStart - 1;
                                    for (int Sample = Channel; Sample < MultichannelUpdateRate; Sample += Channels)
                                        Rendered[Sample] += EchoBuffer[EchoPos = (EchoPos + 1) % Listener.SampleRate] * Volume3D;
                                }
                            }
                        }
                        // ------------------------------------------------------------------
                        // Directional/distance-based engine for asymmetrical layouts
                        // ------------------------------------------------------------------
                    } else {
                        // Angle match calculations
                        bool TheatreMode = AudioListener3D._EnvironmentType == Environments.Theatre;
                        float[] AngleMatches = UsedAngleMatchFunc(Channels, Direction, TheatreMode ? (MatchModifierFunc)PowTo16 : PowTo8);
                        // Object size extension
                        if (Size != 0) {
                            float MaxAngleMatch = CavernUtilities.ArrayMaximum(AngleMatches, Channels);
                            for (int Channel = 0; Channel < Channels; ++Channel)
                                AngleMatches[Channel] = CavernUtilities.FastLerp(AngleMatches[Channel], MaxAngleMatch, Size);
                        }
                        // Only use the closest 3 speakers on non-Perfect qualities or in Theatre mode
                        if (Listener.AudioQuality != QualityModes.Perfect || TheatreMode) {
                            float Top0 = 0, Top1 = 0, Top2 = 0;
                            for (int Channel = 0; Channel < Channels; ++Channel) {
                                if (!AudioListener3D.Channels[Channel].LFE) {
                                    float Match = AngleMatches[Channel];
                                    if (Top0 < Match) { Top2 = Top1; Top1 = Top0; Top0 = Match; }
                                    else if (Top1 < Match) { Top2 = Top1; Top1 = Match; }
                                    else if (Top2 < Match) Top2 = Match;
                                }
                            }
                            for (int Channel = 0; Channel < Channels; ++Channel) {
                                float Match = AngleMatches[Channel];
                                if (!AudioListener3D.Channels[Channel].LFE && Match != Top0 && Match != Top1 && Match != Top2)
                                    AngleMatches[Channel] = 0;
                            }
                        }
                        // Place in sphere, write data to output channels
                        float TotalAngleMatch = 0;
                        for (int Channel = 0; Channel < Channels; ++Channel)
                            TotalAngleMatch += AngleMatches[Channel];
                        float Volume3D = Volume * RolloffDistance * SpatialBlend / TotalAngleMatch;
                        for (int Channel = 0; Channel < Channels; ++Channel) {
                            if (AudioListener3D.Channels[Channel].LFE) {
                                if (OutputRawLFE)
                                    UsedOutputFunc(Samples, Rendered, UpdateRate, Volume3D * TotalAngleMatch, Channel, Channels);
                            } else if (!LFE && AngleMatches[Channel] != 0)
                                UsedOutputFunc(Samples, Rendered, UpdateRate, Volume3D * AngleMatches[Channel], Channel, Channels);
                        }
                        // Add echo from every other direction, if enabled
                        if (!SkipEcho && EchoVolume != 0 && !LFE) {
                            float NewAngleMatch = 0;
                            for (int Channel = 0; Channel < Channels; ++Channel) {
                                AngleMatches[Channel] = TotalAngleMatch - AngleMatches[Channel];
                                NewAngleMatch += AngleMatches[Channel];
                            }
                            Volume3D *= EchoVolume * TotalAngleMatch / NewAngleMatch;
                            int EchoStart = (int)((1f - EchoDelay) * Listener.SampleRate) + ResampledNow;
                            int MultichannelUpdateRate = BaseUpdateRate * Channels;
                            for (int Channel = 0; Channel < Channels; ++Channel) {
                                if (!AudioListener3D.Channels[Channel].LFE) {
                                    float Gain = RolloffDistance * AngleMatches[Channel] * Volume3D;
                                    int EchoPos = EchoStart - 1;
                                    for (int Sample = Channel; Sample < MultichannelUpdateRate; Sample += Channels)
                                        Rendered[Sample] += EchoBuffer[EchoPos = (EchoPos + 1) % Listener.SampleRate] * Gain;
                                }
                            }
                        }
                    }
                }
            }
            // Timing
            timeSamples += PitchedUpdateRate;
            int MaxLength = ClipSamples;
            if (timeSamples >= MaxLength) {
                if (Loop)
                    timeSamples %= MaxLength;
                else {
                    timeSamples = 0;
                    IsPlaying = false;
                }
            }
            return Rendered;
        }
    }
}