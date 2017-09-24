using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern {
    [AddComponentMenu("Audio/3D Audio Source")]
    public partial class AudioSource3D : MonoBehaviour {
        // ------------------------------------------------------------------
        // Internal helpers
        // ------------------------------------------------------------------
        /// <summary>Is the user's speaker layout symmetrical?</summary>
        internal static bool Symmetric = false;

        // ------------------------------------------------------------------
        // Lifecycle helpers
        // ------------------------------------------------------------------
        void Start() {
            if (RandomPosition)
                timeSamples = UnityEngine.Random.Range(0, Clip.samples);
            LastDistance = GetDistance(transform.position);
            LastPosition = transform.position;
        }

        void OnEnable() {
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

        /// <summary>Distance from the listener.</summary>
        float Distance;
        /// <summary>Cached <see cref="EchoVolume"/> after <see cref="AudioListener3D.HeadphoneVirtualizer"/> was set.</summary>
        float OldEchoVolume;
        /// <summary>Cached <see cref="EchoDelay"/> after <see cref="AudioListener3D.HeadphoneVirtualizer"/> was set.</summary>
        float OldEchoDelay;
        /// <summary><see cref="Distance"/> in the previous frame, required for Doppler effect calculation.</summary>
        float LastDistance;
        /// <summary>The last sample past the filter is required for lowpass effects.</summary>
        float LastLowpassedSample = 0;

        /// <summary>Past output samples for echo effect.</summary>
        float[] EchoBuffer = new float[0];

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

        /// <summary>Pitch-shifts a single channel.</summary>
        /// <param name="Samples">Samples of the source channel</param>
        /// <param name="PitchRatio">Pitch ratio</param>
        /// <returns>A pitch-shifted version of the given array with the given pitch ratio</returns>
        static float[] MonoPitchShift(float[] Samples, float PitchRatio) {
            if (PitchRatio == 1)
                return Samples;
            int ProcessEnd = (int)(Samples.Length / PitchRatio) + 1;
            if (ProcessEnd > AudioListener3D.Current.UpdateRate)
                ProcessEnd = AudioListener3D.Current.UpdateRate;
            float[] Output = new float[ProcessEnd];
            if (AudioListener3D.Current.AudioQuality < QualityModes.High) { // No interpolation on qualities below High
                for (int i = 0; i < ProcessEnd; ++i)
                    Output[i] = Samples[(int)(i * PitchRatio)];
            } else {
                int End = Samples.Length - 1;
                for (int i = 0; i < ProcessEnd; ++i) {
                    float FromPos = i * PitchRatio;
                    int Sample = (int)FromPos;
                    Output[i] = Sample >= End ? Samples[Sample] : CavernUtilities.FastLerp(Samples[Sample], Samples[++Sample], FromPos % 1);
                }
            } // TODO: Catmull-Rom on Perfect quality
            return Output;
        }

        /// <summary>Resamples a single channel.</summary>
        /// <param name="Samples">Samples of the source channel</param>
        /// <param name="From">Old sample rate</param>
        /// <param name="To">New sample rate</param>
        /// <returns>Returns a resampled version of the given array</returns>
        internal static float[] Resample(float[] Samples, int From, int To) {
            if (From == To)
                return Samples;
            float[] NewSamples = new float[To];
            float Ratio = From / (float)To;
            if (AudioListener3D.Current.AudioQuality < QualityModes.High) { // No interpolation on qualities below High
                for (int i = 0; i < To; ++i)
                    NewSamples[i] = Samples[(int)(i * Ratio)];
            } else { // Catmull-Rom would be nice if it wouldn't kill the CPU
                int End = From - 1;
                for (int i = 0; i < To; ++i) {
                    float FromPos = i * Ratio;
                    int Sample = (int)FromPos;
                    NewSamples[i] = Sample >= End ? Samples[Sample] : CavernUtilities.FastLerp(Samples[Sample], Samples[++Sample], FromPos % 1);
                }
            }
            return NewSamples;
        }

        /// <summary>Clamp a number between two values.</summary>
        /// <param name="x">Input number</param>
        /// <param name="min">Minimum</param>
        /// <param name="max">Maximum</param>
        /// <returns>X between Minimum and Maximum</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Clamp(float x, float min, float max) { return x < min ? min : (x > max ? max : x); }

        /// <summary>Calculate distance from the <see cref="AudioListener3D"/> and choose the closest sources to play.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Precalculate() {
            if (!Clip || !IsPlaying)
                return;
            Distance = GetDistance(transform.position);
            CavernUtilities.BottomlistHandler(ref AudioListener3D.SourceDistances, AudioListener3D.MaximumSources, Distance);
        }

        /// <summary>Process the source and write to the <see cref="AudioListener3D"/>'s output buffer.</summary>
        /// <param name="UpdatePulse">True, if this is a frame-changing update.</param>
        internal unsafe void Collect(bool UpdatePulse) {
            if (!Clip)
                return;
            AudioListener3D Listener = AudioListener3D.Current;
            if (Delay > 0) {
                Delay -= (ulong)Listener.UpdateRate;
                return;
            }
            // Doppler calculation
            float DopplerPitch = Listener.AudioQuality == QualityModes.Low ? 1 : // Disable any pitch change on low quality
                (DopplerLevel == 0 ? Pitch : Clamp(Pitch * (1f + DopplerLevel * (LastDistance - Distance) *
                0.00294117647058823529411764705882f /* 1 / 340 m/s (speed of sound) */), .5f, 3f));
            if (UpdatePulse) {
                LastDistance = Distance;
                LastPosition = transform.position;
            }
            if (!Clip || !IsPlaying || !CavernUtilities.ArrayContains(ref AudioListener3D.SourceDistances, AudioListener3D.MaximumSources, Distance))
                return;
            bool OutputRawLFE = !Listener.LFESeparation || LFE;
            // Timing
            int UpdateRate = Listener.UpdateRate;
            int PitchedUpdateRate = (int)(UpdateRate * DopplerPitch), BaseUpdateRate = PitchedUpdateRate, ResampledNow = timeSamples;
            bool NeedsResampling = Listener.SampleRate != Clip.frequency;
            if (NeedsResampling) {
                float Mult = (float)Clip.frequency / Listener.SampleRate;
                PitchedUpdateRate = (int)(PitchedUpdateRate * Mult);
                ResampledNow = (int)(timeSamples / Mult);
            }
            timeSamples += PitchedUpdateRate;
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
            if (Mute)
                return;
            bool Blend2D = SpatialBlend != 1, Blend3D = SpatialBlend != 0;
            bool HighQuality = Listener.AudioQuality >= QualityModes.High;
            bool StereoClip = Clip.channels == 2;
            int Channels = AudioListener3D.ChannelCount;
            // Mono mix
            float[] Samples = new float[PitchedUpdateRate];
            if (Blend3D || !StereoClip) {
                int ClipChannels = Clip.channels;
                if (ClipChannels == 1)
                    Clip.GetData(Samples, timeSamples);
                else {
                    float[] OriginalSamples = new float[ClipChannels * PitchedUpdateRate];
                    Clip.GetData(OriginalSamples, timeSamples);
                    if (HighQuality) { // Mono downmix above medium quality
                        int ChannelsToGet;
                        fixed (float* SampleArr = Samples, OriginalArr = OriginalSamples) {
                            float* Sample = SampleArr, OrigSamples = OriginalArr;
                            int SamplesToGet = PitchedUpdateRate;
                            while (SamplesToGet-- != 0) {
                                ChannelsToGet = ClipChannels;
                                while (ChannelsToGet-- != 0)
                                    *Sample += *OrigSamples++;
                                *Sample++ /= ClipChannels;
                            }
                        }
                    } else { // First channel only otherwise
                        int SamplesToGet;
                        fixed (float* SampleArr = Samples, OriginalArr = OriginalSamples) {
                            float* Sample = SampleArr, OrigSamples = OriginalArr - ClipChannels;
                            SamplesToGet = PitchedUpdateRate;
                            while (SamplesToGet-- != 0)
                                *Sample++ = *(OrigSamples += ClipChannels);
                        }
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
                    Volume2D = Divisor == 0 ? 0 : Volume2D / Divisor;
                    if (NeedsResampling)
                        Samples = Resample(Samples, PitchedUpdateRate, BaseUpdateRate);
                    Samples = MonoPitchShift(Samples, DopplerPitch);
                    int ActualSample = 0;
                    for (int Sample = 0; Sample < UpdateRate; ++Sample) {
                        float GainedSample = Samples[Sample] * Volume2D;
                        for (int Channel = 0; Channel < Channels; ++Channel)
                            AudioListener3D.Output[ActualSample++] += GainedSample;
                    }
                } else {
                    float[] StereoSamples = new float[PitchedUpdateRate * 2], LeftSamples = new float[PitchedUpdateRate], RightSamples = new float[PitchedUpdateRate];
                    Clip.GetData(StereoSamples, timeSamples);
                    int ActualSample = 0;
                    for (int Sample = 0; Sample < PitchedUpdateRate; ++Sample) {
                        LeftSamples[Sample] = StereoSamples[ActualSample++];
                        RightSamples[Sample] = StereoSamples[ActualSample++];
                    }
                    if (NeedsResampling) {
                        LeftSamples = Resample(LeftSamples, PitchedUpdateRate, BaseUpdateRate);
                        RightSamples = Resample(RightSamples, PitchedUpdateRate, BaseUpdateRate);
                    }
                    LeftSamples = MonoPitchShift(LeftSamples, DopplerPitch);
                    RightSamples = MonoPitchShift(RightSamples, DopplerPitch);
                    int LeftDivisor = 0, RightDivisor = 0;
                    for (int Channel = 0; Channel < Channels; ++Channel)
                        if (!AudioListener3D.Channels[Channel].LFE) {
                            LeftDivisor += AudioListener3D.Channels[Channel].y < 0 ? 1 : 0;
                            RightDivisor += AudioListener3D.Channels[Channel].y > 0 ? 1 : 0;
                        }
                    float LeftVolume = LeftDivisor == 0 ? 0 : Volume2D / LeftDivisor, RightVolume = RightDivisor == 0 ? 0 : Volume2D / RightDivisor;
                    if (StereoPan < 0)
                        RightVolume *= -StereoPan * StereoPan + 1;
                    else if (StereoPan > 0)
                        LeftVolume *= 1 - StereoPan * StereoPan;
                    float HalfVolume2D = Volume2D * .5f;
                    ActualSample = 0;
                    for (int Sample = 0; Sample < UpdateRate; ++Sample) {
                        float LeftSample = LeftSamples[Sample], RightSample = RightSamples[Sample], LeftGained = LeftSample * LeftVolume, RightGained = RightSample * RightVolume;
                        for (int Channel = 0; Channel < Channels; ++Channel) {
                            if (AudioListener3D.Channels[Channel].LFE) {
                                if (OutputRawLFE)
                                    AudioListener3D.Output[ActualSample] += (LeftSample + RightSample) * HalfVolume2D;
                            } else if (!LFE)
                                AudioListener3D.Output[ActualSample] +=
                                    (AudioListener3D.Channels[Channel].y < 0 ? 1 : 0) * LeftGained + (AudioListener3D.Channels[Channel].y > 0 ? 1 : 0) * RightGained;
                            ++ActualSample;
                        }
                    }
                }
            }
            if (Blend3D && Distance < Listener.Range) { // 3D mix, if the source is in range
                float RolloffDistance;
                switch (VolumeRolloff) {
                    case Rolloffs.Logarithmic:
                        RolloffDistance = Distance < 1 ? 1 : 1 / (1 + Mathf.Log(Distance));
                        break;
                    case Rolloffs.Linear:
                        RolloffDistance = (Listener.Range - Distance) / Listener.Range;
                        break;
                    case Rolloffs.Real:
                        RolloffDistance = Distance < 1 ? 1 : 1 / Distance;
                        break;
                    default:
                        RolloffDistance = 1;
                        break;
                }
                if (NeedsResampling && (!Blend2D || StereoClip)) {
                    Samples = Resample(Samples, PitchedUpdateRate, BaseUpdateRate);
                    PitchedUpdateRate = BaseUpdateRate;
                }
                Samples = MonoPitchShift(Samples, DopplerPitch);
                BaseUpdateRate = Samples.Length;
                // Distance lowpass, if enabled
                if (DistanceLowpass != 0) {
                    float DistanceScale = Distance * DistanceLowpass;
                    if (DistanceScale > 1)
                        CavernUtilities.Lowpass(ref Samples, ref LastLowpassedSample, BaseUpdateRate, 1f - 1f / DistanceScale);
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
                    Vector3 LerpedPosition = CavernUtilities.FastLerp(LastPosition, transform.position, AudioListener3D.DeltaTime);
                    Vector3 Direction = Quaternion.Inverse(Listener.transform.rotation) * (LerpedPosition - AudioListener3D.LastPosition);
                    float DirectionMagnitudeRecip = 1f / (Direction.magnitude + .0001f);
                    if (!CachedEcho) {
                        OldEchoVolume = EchoVolume;
                        OldEchoDelay = EchoDelay;
                        CachedEcho = true;
                    }
                    SkipEcho = EchoVolume == 0;
                    Transform ListenerTransform = Listener.gameObject.transform;
                    Vector3 Forward = ListenerTransform.rotation * ListenerTransform.forward, Upward = ListenerTransform.rotation * ListenerTransform.up;
                    float ForwardScalar = Direction.x * Forward.x + Direction.y * Forward.y + Direction.z * Forward.z,
                        UpwardScalar = Direction.x * Upward.x + Direction.y * Upward.y + Direction.z * Upward.z;
                    EchoVolume = (float)Math.Acos(ForwardScalar / (Forward.magnitude + .0001f) * DirectionMagnitudeRecip) * 0.3183098861837906f; // Set volume by angle difference
                    float UpwardMatch = (float)Math.Acos(UpwardScalar / (Upward.magnitude + .0001f) * DirectionMagnitudeRecip) * 0.3183098861837906f; // 0.318... = 1 / pi
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
                        float ClosestTop = 1.1f, ClosestBottom = -1.1f, ClosestTF = 1.1f, ClosestTR = -1.1f, ClosestBF = 1.1f, ClosestBR = -1.1f; // Closest layers in y/z axes
                        Vector3 Position = Quaternion.Inverse(Listener.transform.rotation) * (transform.position - AudioListener3D.LastPosition);
                        Position.x /= AudioListener3D.EnvironmentSize.x;
                        Position.y /= AudioListener3D.EnvironmentSize.y;
                        Position.z /= AudioListener3D.EnvironmentSize.z;
                        for (int Channel = 0; Channel < Channels; ++Channel) { // Find closest horizontal layers
                            float ChannelY = AudioListener3D.Channels[Channel].CubicalPos.y;
                            if (ChannelY < Position.y) {
                                if (ChannelY > ClosestBottom)
                                    ClosestBottom = ChannelY;
                            } else if (ChannelY < ClosestTop)
                                ClosestTop = ChannelY;
                        }
                        for (int Channel = 0; Channel < Channels; ++Channel) {
                            Vector3 ChannelPos = AudioListener3D.Channels[Channel].CubicalPos;
                            if (ChannelPos.y == ClosestBottom) // Bottom layer
                                AssignHorizontalLayer(Channel, ref BFL, ref BFR, ref BRL, ref BRR, ref ClosestBF, ref ClosestBR, Position, ChannelPos);
                            if (ChannelPos.y == ClosestTop) // Top layer
                                AssignHorizontalLayer(Channel, ref TFL, ref TFR, ref TRL, ref TRR, ref ClosestTF, ref ClosestTR, Position, ChannelPos);
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
                            TopVol = (Position.y - BottomY) / (AudioListener3D.Channels[TFL].CubicalPos.y - BottomY);
                            BottomVol = 1f - TopVol;
                        } else
                            TopVol = BottomVol = .5f;
                        float BFVol = LengthRatio(BRL, BFL, Position.z), BRVol = 1f - BFVol, TFVol = LengthRatio(TRL, TFL, Position.z), TRVol = 1f - TFVol, // Length ratios
                            BFRVol = WidthRatio(BFL, BFR, Position.x), BRRVol = WidthRatio(BRL, BRR, Position.x), // Width ratios
                            TFRVol = WidthRatio(TFL, TFR, Position.x), TRRVol = WidthRatio(TRL, TRR, Position.x);
                        UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D * BottomVol * BFVol * (1f - BFRVol), BFL, Channels);
                        UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D * BottomVol * BFVol * BFRVol, BFR, Channels);
                        UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D * BottomVol * BRVol * (1f - BRRVol), BRL, Channels);
                        UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D * BottomVol * BRVol * BRRVol, BRR, Channels);
                        UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D * TopVol * TFVol * (1f - TFRVol), TFL, Channels);
                        UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D * TopVol * TFVol * TFRVol, TFR, Channels);
                        UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D * TopVol * TRVol * (1f - TRRVol), TRL, Channels);
                        UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D * TopVol * TRVol * TRRVol, TRR, Channels);
                    }
                    // LFE mix
                    if (OutputRawLFE) {
                        for (int Channel = 0; Channel < Channels; ++Channel)
                            if (AudioListener3D.Channels[Channel].LFE)
                                UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D, Channel, Channels);
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
                                    AudioListener3D.Output[Sample] += EchoBuffer[EchoPos = (EchoPos + 1) % Listener.SampleRate] * Volume3D;
                            }
                        }
                    }
                // ------------------------------------------------------------------
                // Directional/distance-based engine for asymmetrical layouts
                // ------------------------------------------------------------------
                } else {
                    // Angle match calculations
                    Vector3 LerpedPosition = CavernUtilities.FastLerp(LastPosition, transform.position, AudioListener3D.DeltaTime);
                    Vector3 Direction = Quaternion.Inverse(Listener.transform.rotation) * (LerpedPosition - AudioListener3D.LastPosition);
                    bool TheatreMode = AudioListener3D.EnvironmentType == Environments.Theatre;
                    float[] AngleMatches;
                    float TotalAngleMatch = 0;
                    if (HighQuality) // Only calculate accurate arc cosine above high quality
                        AngleMatches = CalculateAngleMatches(Channels, Direction, TheatreMode ? (Func<float, float>)PowTo16 : PowTo8);
                    else
                        AngleMatches = LinearizeAngleMatches(Channels, Direction, TheatreMode ? (Func<float, float>)PowTo16 : PowTo8);
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
                    for (int Channel = 0; Channel < Channels; ++Channel)
                        TotalAngleMatch += AngleMatches[Channel];
                    float Volume3D = Volume * RolloffDistance * SpatialBlend / TotalAngleMatch;
                    for (int Channel = 0; Channel < Channels; ++Channel) {
                        if (AudioListener3D.Channels[Channel].LFE) {
                            if (OutputRawLFE)
                                UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D * TotalAngleMatch, Channel, Channels);
                        } else if (!LFE && AngleMatches[Channel] != 0)
                            UsedOutputFunc(ref Samples, ref AudioListener3D.Output, UpdateRate, Volume3D * AngleMatches[Channel], Channel, Channels);
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
                                    AudioListener3D.Output[Sample] += EchoBuffer[EchoPos = (EchoPos + 1) % Listener.SampleRate] * Gain;
                            }
                        }
                    }
                }
            }
        }
    }
}