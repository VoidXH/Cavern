using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern {
    public partial class Source {
        // ------------------------------------------------------------------
        // Constants
        // ------------------------------------------------------------------
        /// <summary>Reference sound velocity in m/s (dry air, 25.4 degrees Celsius).</summary>
        public const float SpeedOfSound = 346.74f;

        // ------------------------------------------------------------------
        // Internal helpers
        // ------------------------------------------------------------------
        /// <summary>The <see cref="Listener"/> this source is attached to.</summary>
        internal Listener listener;
        /// <summary>Cached node from <see cref="Listener.activeSources"/> for faster detach.</summary>
        internal LinkedListNode<Source> listenerNode;
        /// <summary>Distance from the listener.</summary>
        internal float distance = float.NaN;

        // ------------------------------------------------------------------
        // Private vars
        // ------------------------------------------------------------------
        /// <summary><see cref="pitchedUpdateRate"/> without resampling.</summary>
        int baseUpdateRate;
        /// <summary>Samples required to match the listener's update rate after pitch changes.</summary>
        int pitchedUpdateRate;

        /// <summary>Actually used pitch multiplier including the Doppler effect.</summary>
        float calculatedPitch;
        /// <summary><see cref="distance"/> in the previous frame, required for Doppler effect calculation.</summary>
        float lastDistance;
        /// <summary>Sample rate multiplier to match the system sample rate.</summary>
        float resampleMult;

        /// <summary>Stereo mix cache to save allocation times.</summary>
        float[] leftSamples = new float[0], rightSamples = new float[0];
        /// <summary>Rendered output array kept to save allocation time.</summary>
        float[] rendered = new float[0];
        /// <summary>Mono mix cache to save allocation times.</summary>
        float[] samples = new float[0];

        /// <summary>Random number generator.</summary>
        Random random = new Random();

        /// <summary>Remaining delay until starting playback.</summary>
        ulong delay = 0;

        /// <summary>Calculate distance from the <see cref="Listener"/> and choose the closest sources to play.</summary>
        internal void Precalculate() {
            if (Renderable) {
                lastDistance = distance;
                distance = Position.Distance(listener.Position);
                if (float.IsNaN(lastDistance))
                    lastDistance = distance;
                Utils.BottomlistHandler(listener.sourceDistances, distance);
            } else
                distance = float.NaN;
        }

        /// <summary>Get the next samples in the audio stream.</summary>
        protected internal virtual float[][] GetSamples() {
            int channels = Clip.Channels;
            if (Rendered == null || Rendered.Length != channels) {
                Rendered = new float[channels][];
                Rendered[0] = new float[0];
            }
            if (Rendered[0].Length != pitchedUpdateRate)
                for (int channel = 0; channel < channels; ++channel)
                    Rendered[channel] = new float[pitchedUpdateRate];
            Clip.GetData(Rendered, TimeSamples);
            return Rendered;
        }

        /// <summary>Quickly checks if a value is in an array.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="count">Array length</param>
        /// <param name="value">Value to check</param>
        /// <returns>If an array contains the value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ArrayContains(float[] target, int count, float value) {
            for (int entry = 0; entry < count; ++entry)
                if (target[entry] == value)
                    return true;
            return false;
        }

        /// <summary>Cache the samples if the source should be rendered. This wouldn't be thread safe.</summary>
        /// <returns>The collection should be performed, as all requirements are met</returns>
        protected internal virtual bool Precollect() {
            if (RandomPosition) {
                TimeSamples = random.Next(0, Clip.Samples);
                RandomPosition = false;
            }
            if (ArrayContains(listener.sourceDistances, listener.MaximumSources, distance)) {
                if (listener.AudioQuality != QualityModes.Low) {
                    if (DopplerLevel == 0)
                        calculatedPitch = Pitch;
                    else
                        calculatedPitch = Utils.Clamp(Pitch * DopplerLevel * SpeedOfSound / (SpeedOfSound -
                            (lastDistance - distance) / listener.pulseDelta), .5f, 3f);
                } else
                    calculatedPitch = 1; // Disable any pitch change on low quality
                bool needsResampling = listener.SampleRate != Clip.SampleRate;
                if (needsResampling)
                    resampleMult = (float)Clip.SampleRate / listener.SampleRate;
                else
                    resampleMult = 1;
                baseUpdateRate = (int)(listener.UpdateRate * calculatedPitch);
                pitchedUpdateRate = (int)(baseUpdateRate * resampleMult);
                if (samples.Length != pitchedUpdateRate)
                    samples = new float[pitchedUpdateRate];
                if (Clip.Channels == 2 && leftSamples.Length != pitchedUpdateRate) {
                    leftSamples = new float[pitchedUpdateRate];
                    rightSamples = new float[pitchedUpdateRate];
                }
                Rendered = GetSamples();
                int outputLength = Listener.Channels.Length * listener.UpdateRate;
                if (rendered.Length != outputLength)
                    rendered = new float[outputLength];
                if (delay > 0)
                    delay -= (ulong)listener.UpdateRate;
                return true;
            }
            Rendered = null;
            return false;
        }

        /// <summary>Output samples to a multichannel array. Automatically applies constant power mixing.</summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="target">Channel array to write to</param>
        /// <param name="gain">Source gain</param>
        /// <param name="channel">Channel ID</param>
        /// <param name="channels">Total channels</param>
        /// <remarks>It is assumed that the size of <paramref name="target"/> equals the size of
        /// <paramref name="samples"/> * <paramref name="channels"/>.</remarks>
        internal static void WriteOutput(float[] samples, float[] target, float gain, int channel, int channels) {
            gain = (float)Math.Sin(Math.PI * .5 * gain);
            for (int from = 0, to = channel, end = samples.Length; from < end; ++from, to += channels)
                target[to] += samples[from] * gain;
        }

        /// <summary>Output samples to all channels of a multichannel array.</summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="target">Channel array to write to</param>
        /// <param name="gain">Source gain, total across all channels</param>
        /// <param name="channels">Total channels</param>
        /// <remarks>It is assumed that the size of <paramref name="target"/> equals the size of
        /// <paramref name="samples"/> * <paramref name="channels"/>.</remarks>
        internal static void WriteOutput(float[] samples, float[] target, float gain, int channels) {
            gain /= channels;
            for (int channel = 0; channel < channels; ++channel)
                for (int from = 0, to = channel, sampleCount = samples.Length; from < sampleCount; ++from, to += channels)
                    target[to] = samples[from] * gain;
        }

        /// <summary>Process the source and returns a mix to be added to the output.</summary>
        protected internal virtual float[] Collect() {
            int channels = Listener.Channels.Length;
            Array.Clear(rendered, 0, rendered.Length);
            bool rawLFE = !listener.LFESeparation || LFE;
            // Update rate calculation
            int updateRate = listener.UpdateRate;
            int resampledNow = (int)(TimeSamples / resampleMult);
            if (!Mute) {
                bool blend2D = SpatialBlend != 1, blend3D = SpatialBlend != 0, highQuality = listener.AudioQuality >= QualityModes.High;
                int clipChannels = Clip.Channels;
                // Mono mix
                if (blend3D) {
                    if (clipChannels == 1)
                        Buffer.BlockCopy(Rendered[0], 0, samples, 0, pitchedUpdateRate * sizeof(float));
                    else {
                        if (highQuality) { // Mono downmix above medium quality
                            Array.Clear(samples, 0, pitchedUpdateRate);
                            float clipChDiv = 1f / clipChannels;
                            for (int channel = 0; channel < clipChannels; ++channel) {
                                float[] sampleSource = Rendered[channel];
                                for (int sample = 0; sample < pitchedUpdateRate; ++sample)
                                    samples[sample] += sampleSource[sample];
                            }
                            for (int sample = 0; sample < pitchedUpdateRate; ++sample)
                                samples[sample] *= clipChDiv;
                        } else { // First channel only otherwise
                            float[] sampleSource = Rendered[0];
                            for (int sample = 0; sample < pitchedUpdateRate; ++sample)
                                samples[sample] = sampleSource[sample];
                        }
                    }
                }
                if (blend2D) { // 2D mix
                    float volume2D = Volume * (1f - SpatialBlend);
                    if (Clip.Channels != 2) {
                        samples = Resample.Adaptive(samples, updateRate, listener.AudioQuality);
                        WriteOutput(samples, rendered, volume2D, channels);
                    } else {
                        Buffer.BlockCopy(Rendered[0], 0, leftSamples, 0, pitchedUpdateRate * sizeof(float));
                        Buffer.BlockCopy(Rendered[1], 0, rightSamples, 0, pitchedUpdateRate * sizeof(float));
                        leftSamples = Resample.Adaptive(leftSamples, updateRate, listener.AudioQuality);
                        rightSamples = Resample.Adaptive(rightSamples, updateRate, listener.AudioQuality);
                        int leftDivisor = 0, rightDivisor = 0;
                        for (int channel = 0; channel < channels; ++channel) {
                            Channel currentChannel = Listener.Channels[channel];
                            if (!currentChannel.LFE) {
                                if (currentChannel.Y < 0)
                                    leftDivisor += 1;
                                else if (currentChannel.Y > 0)
                                    rightDivisor += 1;
                            }
                        }
                        float leftVolume = leftDivisor == 0 ? 0 : volume2D / leftDivisor, rightVolume = rightDivisor == 0 ? 0 : volume2D / rightDivisor;
                        if (stereoPan < 0)
                            rightVolume *= -stereoPan * stereoPan + 1;
                        else if (stereoPan > 0)
                            leftVolume *= 1 - stereoPan * stereoPan;
                        float halfVolume2D = volume2D * .5f;
                        int actualSample = 0;
                        for (int sample = 0; sample < updateRate; ++sample) {
                            float leftSample = leftSamples[sample], rightSample = rightSamples[sample],
                                leftGained = leftSample * leftVolume, rightGained = rightSample * rightVolume;
                            for (int channel = 0; channel < channels; ++channel) {
                                if (Listener.Channels[channel].LFE) {
                                    if (rawLFE)
                                        rendered[actualSample] += (leftSample + rightSample) * halfVolume2D;
                                } else if (!LFE) {
                                    if (Listener.Channels[channel].Y < 0)
                                        rendered[actualSample] += leftGained;
                                    else if (Listener.Channels[channel].Y > 0)
                                        rendered[actualSample] += rightGained;
                                }
                                ++actualSample;
                            }
                        }
                    }
                }
                if (blend3D && distance < listener.Range) { // 3D mix, if the source is in range
                    Vector direction = Position - listener.Position;
                    direction.RotateInverse(listener.Rotation);
                    float rolloffDistance = GetRolloff();
                    samples = Resample.Adaptive(samples, updateRate, listener.AudioQuality);
                    baseUpdateRate = samples.Length;
                    // Apply filter if set
                    if (SpatialFilter != null)
                        SpatialFilter.Process(samples);
                    // ------------------------------------------------------------------
                    // Balance-based engine for symmetrical layouts
                    // ------------------------------------------------------------------
                    if (Listener.IsSymmetric) {
                        float volume3D = Volume * rolloffDistance * SpatialBlend;
                        if (!LFE) {
                            // Find closest channels by cubical position in each direction (bottom/top, front/rear, left/right)
                            int BFL = -1, BFR = -1, BRL = -1, BRR = -1, TFL = -1, TFR = -1, TRL = -1, TRR = -1;
                            float closestTop = 69, closestBottom = -83, closestTF = 90, closestTR = -84,
                                closestBF = 69, closestBR = -82; // Closest layers on y/z
                            direction.Downscale(Listener.EnvironmentSize);
                            for (int channel = 0; channel < channels; ++channel) { // Find closest horizontal layers
                                if (!Listener.Channels[channel].LFE) {
                                    float channelY = Listener.Channels[channel].CubicalPos.y;
                                    if (channelY < direction.y) {
                                        if (channelY > closestBottom)
                                            closestBottom = channelY;
                                    } else if (channelY < closestTop)
                                        closestTop = channelY;
                                }
                            }
                            for (int channel = 0; channel < channels; ++channel) {
                                if (!Listener.Channels[channel].LFE) {
                                    Vector channelPos = Listener.Channels[channel].CubicalPos;
                                    if (channelPos.y == closestBottom) // Bottom layer
                                        AssignHorizontalLayer(channel, ref BFL, ref BFR, ref BRL, ref BRR, ref closestBF, ref closestBR, direction, channelPos);
                                    if (channelPos.y == closestTop) // Top layer
                                        AssignHorizontalLayer(channel, ref TFL, ref TFR, ref TRL, ref TRR, ref closestTF, ref closestTR, direction, channelPos);
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
                            float topVol, bottomVol;
                            if (TFL != BFL) { // Height ratio calculation
                                float bottomY = Listener.Channels[BFL].CubicalPos.y;
                                topVol = (direction.y - bottomY) / (Listener.Channels[TFL].CubicalPos.y - bottomY);
                                bottomVol = 1f - topVol;
                            } else
                                topVol = bottomVol = .5f;
                            float BFVol = LengthRatio(BRL, BFL, direction.z), TFVol = LengthRatio(TRL, TFL, direction.z), // Length ratios
                                BFRVol = WidthRatio(BFL, BFR, direction.x), BRRVol = WidthRatio(BRL, BRR, direction.x), // Width ratios
                                TFRVol = WidthRatio(TFL, TFR, direction.x), TRRVol = WidthRatio(TRL, TRR, direction.x),
                                innerVolume3D = volume3D;
                            if (Size != 0) {
                                BFVol = Utils.Lerp(BFVol, .5f, Size);
                                TFVol = Utils.Lerp(TFVol, .5f, Size);
                                BFRVol = Utils.Lerp(BFRVol, .5f, Size);
                                BRRVol = Utils.Lerp(BRRVol, .5f, Size);
                                TFRVol = Utils.Lerp(TFRVol, .5f, Size);
                                TRRVol = Utils.Lerp(TRRVol, .5f, Size);
                                innerVolume3D *= 1f - Size;
                                float extraChannelVolume = volume3D * Size / channels;
                                for (int channel = 0; channel < channels; ++channel)
                                    WriteOutput(samples, rendered, extraChannelVolume, channel, channels);
                            }
                            float BRVol = 1f - BFVol, TRVol = 1f - TFVol; // Remaining length ratios
                            bottomVol *= innerVolume3D; topVol *= innerVolume3D; BFVol *= bottomVol; BRVol *= bottomVol; TFVol *= topVol; TRVol *= topVol;
                            WriteOutput(samples, rendered, BFVol * (1f - BFRVol), BFL, channels);
                            WriteOutput(samples, rendered, BFVol * BFRVol, BFR, channels);
                            WriteOutput(samples, rendered, BRVol * (1f - BRRVol), BRL, channels);
                            WriteOutput(samples, rendered, BRVol * BRRVol, BRR, channels);
                            WriteOutput(samples, rendered, TFVol * (1f - TFRVol), TFL, channels);
                            WriteOutput(samples, rendered, TFVol * TFRVol, TFR, channels);
                            WriteOutput(samples, rendered, TRVol * (1f - TRRVol), TRL, channels);
                            WriteOutput(samples, rendered, TRVol * TRRVol, TRR, channels);
                        }
                        // LFE mix
                        if (rawLFE)
                            for (int channel = 0; channel < channels; ++channel)
                                if (Listener.Channels[channel].LFE)
                                    WriteOutput(samples, rendered, volume3D, channel, channels);
                    // ------------------------------------------------------------------
                    // Directional/distance-based engine for asymmetrical layouts
                    // ------------------------------------------------------------------
                    } else {
                        // Angle match calculations
                        float[] angleMatches;
                        if (listener.AudioQuality >= QualityModes.High)
                            angleMatches = CalculateAngleMatches(channels, direction,
                                Listener.EnvironmentType == Environments.Theatre ? (MatchModifierFunc)PowTo16 : PowTo8);
                        else
                            angleMatches = LinearizeAngleMatches(channels, direction,
                                Listener.EnvironmentType == Environments.Theatre ? (MatchModifierFunc)PowTo16 : PowTo8);
                        // Object size extension
                        if (Size != 0) {
                            float maxAngleMatch = angleMatches[0];
                            for (int channel = 1; channel < channels; ++channel)
                                if (maxAngleMatch < angleMatches[channel])
                                    maxAngleMatch = angleMatches[channel];
                            for (int channel = 0; channel < channels; ++channel)
                                angleMatches[channel] = Utils.Lerp(angleMatches[channel], maxAngleMatch, Size);
                        }
                        // Only use the closest 3 speakers on non-Perfect qualities or in Theatre mode
                        if (listener.AudioQuality != QualityModes.Perfect || Listener.EnvironmentType == Environments.Theatre) {
                            float top0 = 0, top1 = 0, top2 = 0;
                            for (int channel = 0; channel < channels; ++channel) {
                                if (!Listener.Channels[channel].LFE) {
                                    float match = angleMatches[channel];
                                    if (top0 < match) { top2 = top1; top1 = top0; top0 = match; }
                                    else if (top1 < match) { top2 = top1; top1 = match; } else if (top2 < match) top2 = match;
                                }
                            }
                            for (int channel = 0; channel < channels; ++channel) {
                                float match = angleMatches[channel];
                                if (!Listener.Channels[channel].LFE && match != top0 && match != top1 && match != top2)
                                    angleMatches[channel] = 0;
                            }
                        }
                        // Place in sphere, write data to output channels
                        float totalAngleMatch = 0;
                        for (int channel = 0; channel < channels; ++channel)
                            totalAngleMatch += angleMatches[channel];
                        float volume3D = Volume * rolloffDistance * SpatialBlend / totalAngleMatch;
                        for (int channel = 0; channel < channels; ++channel) {
                            if (Listener.Channels[channel].LFE) {
                                if (rawLFE)
                                    WriteOutput(samples, rendered, volume3D * totalAngleMatch, channel, channels);
                            } else if (!LFE && angleMatches[channel] != 0)
                                WriteOutput(samples, rendered, volume3D * angleMatches[channel], channel, channels);
                        }
                    }
                }
            }
            // Timing
            TimeSamples += pitchedUpdateRate;
            if (TimeSamples >= Clip.Samples) {
                if (Loop)
                    TimeSamples %= Clip.Samples;
                else {
                    TimeSamples = 0;
                    IsPlaying = false;
                }
            }
            return rendered;
        }
    }
}