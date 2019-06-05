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

        // ------------------------------------------------------------------
        // Internal helpers
        // ------------------------------------------------------------------
        /// <summary>Is the user's speaker layout symmetrical?</summary>
        internal static bool Symmetric = false;
        /// <summary>Distance from the listener.</summary>
        internal float Distance = float.NaN;
        /// <summary>Indicates that the source meets rendering requirements, and <see cref="GetSamples"/> won't fail.</summary>
        internal virtual bool Renderable => IsPlaying && CavernClip;

        // ------------------------------------------------------------------
        // Lifecycle helpers
        // ------------------------------------------------------------------
        void OnEnable() {
            if (RandomPosition)
                timeSamples = UnityEngine.Random.Range(0, CavernClip.Samples);
            Distance = GetDistance(transform.position);
            SetRolloff();
            Node = AudioListener3D.ActiveSources.AddLast(this);
        }

        void OnDisable() => Node.List.Remove(Node);

        // ------------------------------------------------------------------
        // Private vars
        // ------------------------------------------------------------------
        /// <summary><see cref="PitchedUpdateRate"/> without resampling.</summary>
        int BaseUpdateRate;
        /// <summary>Hash code of the last imported <see cref="AudioClip"/> that has been converted to <see cref="Cavern.Clip"/>.</summary>
        int LastClipHash;
        /// <summary>Samples required to match the listener's update rate after pitch changes.</summary>
        int PitchedUpdateRate;

        /// <summary>Actually used pitch multiplier including the Doppler effect.</summary>
        float CalculatedPitch;
        /// <summary><see cref="Distance"/> in the previous frame, required for Doppler effect calculation.</summary>
        float LastDistance;
        /// <summary>Sample rate multiplier to match the system sample rate.</summary>
        float ResampleMult;

        /// <summary>Stereo mix cache to save allocation times.</summary>
        float[] LeftSamples = new float[0], RightSamples = new float[0];
        /// <summary>Rendered output array kept to save allocation time.</summary>
        float[] Rendered = new float[0];
        /// <summary>Mono mix cache to save allocation times.</summary>
        float[] Samples = new float[0];
        /// <summary>Sample buffer from the clip.</summary>
        float[][] OriginalSamples;

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
        /// <param name="From">Old sample count</param>
        /// <param name="To">New sample count</param>
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

        /// <summary>Calculate distance from the <see cref="AudioListener3D"/> and choose the closest sources to play.</summary>
        internal void Precalculate() {
            if (Clip && (!CavernClip || LastClipHash != Clip.GetHashCode())) {
                float[] AllData = new float[Clip.channels * Clip.samples];
                Clip.GetData(AllData, 0);
                CavernClip = new Clip(AllData, Clip.channels, Clip.frequency);
                LastClipHash = Clip.GetHashCode();
            }
            if (Renderable) {
                LastPosition = transform.position;
                LastDistance = Distance;
                Distance = GetDistance(LastPosition);
                CavernUtilities.BottomlistHandler(AudioListener3D.SourceDistances, AudioListener3D.MaximumSources, Distance);
            } else
                Distance = float.NaN;
        }

        /// <summary>Get the next samples in the audio stream.</summary>
        internal virtual float[][] GetSamples() {
            int channels = CavernClip.Channels;
            OriginalSamples = new float[channels][];
            for (int channel = 0; channel < channels; ++channel)
                OriginalSamples[channel] = new float[PitchedUpdateRate];
            CavernClip.GetData(OriginalSamples, timeSamples);
            return OriginalSamples;
        }

        /// <summary>Quickly checks if a value is in an array.</summary>
        /// <param name="Target">Array reference</param>
        /// <param name="Count">Array length</param>
        /// <param name="Value">Value to check</param>
        /// <returns>If an array contains the value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ArrayContains(float[] Target, int Count, float Value) {
            for (int Entry = 0; Entry < Count; ++Entry)
                if (Target[Entry] == Value)
                    return true;
            return false;
        }

        /// <summary>Cache the samples if the source should be rendered. This wouldn't be thread safe.</summary>
        /// <returns>The collection should be performed, as all requirements are met</returns>
        internal virtual bool Precollect() {
            if (ArrayContains(AudioListener3D.SourceDistances, AudioListener3D.MaximumSources, Distance)) {
                AudioListener3D Listener = AudioListener3D.Current;
                if (Listener.AudioQuality != QualityModes.Low) {
                    if (DopplerLevel == 0)
                        CalculatedPitch = Pitch;
                    else
                        CalculatedPitch = Mathf.Clamp(Pitch * DopplerLevel * CavernUtilities.SpeedOfSound / (CavernUtilities.SpeedOfSound -
                            (LastDistance - Distance) / AudioListener3D.PulseDelta), .5f, 3f);
                } else
                    CalculatedPitch = 1; // Disable any pitch change on low quality
                bool NeedsResampling = Listener.SampleRate != CavernClip.SampleRate;
                if (NeedsResampling)
                    ResampleMult = (float)CavernClip.SampleRate / Listener.SampleRate;
                else
                    ResampleMult = 1;
                BaseUpdateRate = (int)(Listener.UpdateRate * CalculatedPitch);
                PitchedUpdateRate = (int)(BaseUpdateRate * ResampleMult);
                if (Samples.Length != PitchedUpdateRate)
                    Samples = new float[PitchedUpdateRate];
                if (CavernClip.Channels == 2 && LeftSamples.Length != PitchedUpdateRate) {
                    LeftSamples = new float[PitchedUpdateRate];
                    RightSamples = new float[PitchedUpdateRate];
                }
                OriginalSamples = GetSamples();
                if (Rendered.Length != AudioListener3D.RenderBufferSize)
                    Rendered = new float[AudioListener3D.RenderBufferSize];
                if (Delay > 0)
                    Delay -= (ulong)Listener.UpdateRate;
                return true;
            }
            OriginalSamples = null;
            return false;
        }

        /// <summary>Output samples to a multichannel array.</summary>
        /// <param name="Samples">Samples to write</param>
        /// <param name="Target">Channel array to write to</param>
        /// <param name="ChannelLength">Size of the source and destination arrays</param>
        /// <param name="Gain">Source gain</param>
        /// <param name="Channel">Channel ID</param>
        /// <param name="Channels">Total channels</param>
        internal static void WriteOutput(float[] Samples, float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
            Gain = Mathf.Sin(Mathf.PI * .5f * Gain);
            for (int From = 0, To = Channel; From < ChannelLength; ++From, To += Channels)
                Target[To] += Samples[From] * Gain;
        }

        /// <summary>Process the source and returns a mix to be added to the output.</summary>
        internal virtual float[] Collect() {
            AudioListener3D Listener = AudioListener3D.Current;
            int Channels = AudioListener3D.ChannelCount;
            Array.Clear(Rendered, 0, Rendered.Length);
            bool OutputRawLFE = !Listener.LFESeparation || LFE;
            // Update rate calculation
            int UpdateRate = Listener.UpdateRate;
            int ResampledNow = (int)(timeSamples / ResampleMult);
            if (!Mute) {
                bool Blend2D = SpatialBlend != 1, Blend3D = SpatialBlend != 0;
                bool HighQuality = Listener.AudioQuality >= QualityModes.High;
                int ClipChannels = CavernClip.Channels;
                bool StereoClip = ClipChannels == 2;
                // Mono mix
                if (Blend3D || !StereoClip) {
                    if (ClipChannels == 1)
                        Buffer.BlockCopy(OriginalSamples[0], 0, Samples, 0, PitchedUpdateRate * sizeof(float));
                    else {
                        if (HighQuality) { // Mono downmix above medium quality
                            Array.Clear(Samples, 0, PitchedUpdateRate);
                            float ClipChDiv = 1f / ClipChannels;
                            for (int channel = 0; channel < ClipChannels; ++channel) {
                                float[] sampleSource = OriginalSamples[channel];
                                for (int sample = 0; sample < PitchedUpdateRate; ++sample)
                                    Samples[sample] += sampleSource[sample];
                            }
                        } else { // First channel only otherwise
                            float[] sampleSource = OriginalSamples[0];
                            for (int sample = 0; sample < PitchedUpdateRate; ++sample)
                                Samples[sample] = sampleSource[sample];
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
                        Buffer.BlockCopy(OriginalSamples[0], 0, LeftSamples, 0, PitchedUpdateRate * sizeof(float));
                        Buffer.BlockCopy(OriginalSamples[1], 0, RightSamples, 0, PitchedUpdateRate * sizeof(float));
                        LeftSamples = Resample(LeftSamples, LeftSamples.Length, UpdateRate);
                        RightSamples = Resample(RightSamples, RightSamples.Length, UpdateRate);
                        int LeftDivisor = 0, RightDivisor = 0;
                        for (int Channel = 0; Channel < Channels; ++Channel) {
                            Channel3D CurrentChannel = AudioListener3D.Channels[Channel];
                            if (!CurrentChannel.LFE) {
                                if (CurrentChannel.Y < 0)
                                    LeftDivisor += 1;
                                else if (CurrentChannel.Y > 0)
                                    RightDivisor += 1;
                            }
                        }
                        float LeftVolume = LeftDivisor == 0 ? 0 : Volume2D / LeftDivisor, RightVolume = RightDivisor == 0 ? 0 : Volume2D / RightDivisor;
                        if (StereoPan < 0)
                            RightVolume *= -StereoPan * StereoPan + 1;
                        else if (StereoPan > 0)
                            LeftVolume *= 1 - StereoPan * StereoPan;
                        float HalfVolume2D = Volume2D * .5f;
                        int ActualSample = 0;
                        for (int Sample = 0; Sample < UpdateRate; ++Sample) {
                            float LeftSample = LeftSamples[Sample], RightSample = RightSamples[Sample],
                                LeftGained = LeftSample * LeftVolume, RightGained = RightSample * RightVolume;
                            for (int Channel = 0; Channel < Channels; ++Channel) {
                                if (AudioListener3D.Channels[Channel].LFE) {
                                    if (OutputRawLFE)
                                        Rendered[ActualSample] += (LeftSample + RightSample) * HalfVolume2D;
                                } else if (!LFE) {
                                    if (AudioListener3D.Channels[Channel].Y < 0)
                                        Rendered[ActualSample] += LeftGained;
                                    else if (AudioListener3D.Channels[Channel].Y > 0)
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
                    // Apply filter if set
                    if (SpatialFilter != null)
                        SpatialFilter.Process(Samples);
                    // ------------------------------------------------------------------
                    // Balance-based engine for symmetrical layouts
                    // ------------------------------------------------------------------
                    if (Symmetric) {
                        float Volume3D = Volume * RolloffDistance * SpatialBlend;
                        if (!LFE) {
                            // Find closest channels by cubical position in each direction (bottom/top, front/rear, left/right)
                            int BFL = -1, BFR = -1, BRL = -1, BRR = -1, TFL = -1, TFR = -1, TRL = -1, TRR = -1;
                            float ClosestTop = 69, ClosestBottom = -83, ClosestTF = 90, ClosestTR = -84,
                                ClosestBF = 69, ClosestBR = -82; // Closest layers on y/z
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
                                    WriteOutput(Samples, Rendered, UpdateRate, ExtraChannelVolume, Channel, Channels);
                            }
                            float BRVol = 1f - BFVol, TRVol = 1f - TFVol; // Remaining length ratios
                            BottomVol *= InnerVolume3D; TopVol *= InnerVolume3D; BFVol *= BottomVol; BRVol *= BottomVol; TFVol *= TopVol; TRVol *= TopVol;
                            WriteOutput(Samples, Rendered, UpdateRate, BFVol * (1f - BFRVol), BFL, Channels);
                            WriteOutput(Samples, Rendered, UpdateRate, BFVol * BFRVol, BFR, Channels);
                            WriteOutput(Samples, Rendered, UpdateRate, BRVol * (1f - BRRVol), BRL, Channels);
                            WriteOutput(Samples, Rendered, UpdateRate, BRVol * BRRVol, BRR, Channels);
                            WriteOutput(Samples, Rendered, UpdateRate, TFVol * (1f - TFRVol), TFL, Channels);
                            WriteOutput(Samples, Rendered, UpdateRate, TFVol * TFRVol, TFR, Channels);
                            WriteOutput(Samples, Rendered, UpdateRate, TRVol * (1f - TRRVol), TRL, Channels);
                            WriteOutput(Samples, Rendered, UpdateRate, TRVol * TRRVol, TRR, Channels);
                        }
                        // LFE mix
                        if (OutputRawLFE) {
                            for (int Channel = 0; Channel < Channels; ++Channel)
                                if (AudioListener3D.Channels[Channel].LFE)
                                    WriteOutput(Samples, Rendered, UpdateRate, Volume3D, Channel, Channels);
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
                            float MaxAngleMatch = AngleMatches[0];
                            for (int Channel = 1; Channel < Channels; ++Channel)
                                if (MaxAngleMatch < AngleMatches[Channel])
                                    MaxAngleMatch = AngleMatches[Channel];
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
                                    WriteOutput(Samples, Rendered, UpdateRate, Volume3D * TotalAngleMatch, Channel, Channels);
                            } else if (!LFE && AngleMatches[Channel] != 0)
                                WriteOutput(Samples, Rendered, UpdateRate, Volume3D * AngleMatches[Channel], Channel, Channels);
                        }
                    }
                }
            }
            // Timing
            timeSamples += PitchedUpdateRate;
            int MaxLength = CavernClip.Samples;
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