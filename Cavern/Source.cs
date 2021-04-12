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
        protected internal Listener listener;
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
        readonly Random random = new Random();

        /// <summary>Remaining delay until starting playback.</summary>
        long delay = 0;

        /// <summary>Keeps a value in the given array, if it's smaller than any of its contents.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="value">Value to insert</param>
        static void BottomlistHandler(float[] target, float value) {
            int replace = -1;
            for (int record = 0; record < target.Length; ++record)
                if (target[record] > value)
                    replace = replace == -1 ? record : (target[record] > target[replace] ? record : replace);
            if (replace != -1)
                target[replace] = value;
        }

        /// <summary>Calculate distance from the <see cref="Listener"/> and choose the closest sources to play.</summary>
        internal void Precalculate() {
            if (Renderable) {
                lastDistance = distance;
                distance = Position.Distance(listener.Position);
                if (float.IsNaN(lastDistance))
                    lastDistance = distance;
                BottomlistHandler(listener.sourceDistances, distance);
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
            if (Loop)
                Clip.GetData(Rendered, TimeSamples);
            else
                Clip.GetDataNonLooping(Rendered, TimeSamples);
            return Rendered;
        }

        /// <summary>Quickly checks if a value is in an array.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="value">Value to check</param>
        /// <returns>If an array contains the value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ArrayContains(float[] target, float value) {
            for (int entry = 0; entry < target.Length; ++entry)
                if (target[entry] == value)
                    return true;
            return false;
        }

        /// <summary>Cache the samples if the source should be rendered. This wouldn't be thread safe.</summary>
        /// <returns>The collection should be performed, as all requirements are met</returns>
        protected internal virtual bool Precollect() {
            if (delay > 0) {
                delay -= listener.UpdateRate;
                return false;
            }
            if (ArrayContains(listener.sourceDistances, distance)) {
                if (listener.AudioQuality != QualityModes.Low) {
                    if (DopplerLevel == 0)
                        calculatedPitch = Pitch;
                    else
                        calculatedPitch = QMath.Clamp(Pitch * DopplerLevel * SpeedOfSound / (SpeedOfSound -
                            (lastDistance - distance) / listener.pulseDelta), .5f, 3f);
                } else
                    calculatedPitch = 1; // Disable any pitch change on low quality
                if (listener.SampleRate != Clip.SampleRate)
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
                if (rendered.Length != Listener.Channels.Length * listener.UpdateRate)
                    rendered = new float[Listener.Channels.Length * listener.UpdateRate];
                return true;
            }
            Rendered = null;
            return false;
        }

        /// <summary>Makes sure if <see cref="Precollect"/> is called immediatly after this function, it will return true.</summary>
        protected void ForcePrecollect() => listener.sourceDistances[0] = distance;

        /// <summary>Output samples to a multichannel array. Automatically applies constant power mixing.</summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="target">Channel array to write to</param>
        /// <param name="gain">Source gain</param>
        /// <param name="channel">Channel ID</param>
        /// <param name="channels">Total channels</param>
        /// <remarks>It is assumed that the size of <paramref name="target"/> equals the size of
        /// <paramref name="samples"/> * <paramref name="channels"/>.</remarks>
        internal static void WriteOutput(float[] samples, float[] target, float gain, int channel, int channels) {
            gain = (float)Math.Sqrt(gain);
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

        void Stereo2DMix(float volume2D) {
            float leftVolume = volume2D / Listener.LeftChannels,
                rightVolume = volume2D / Listener.RightChannels;
            if (stereoPan < 0)
                rightVolume *= -stereoPan * stereoPan + 1;
            else if (stereoPan > 0)
                leftVolume *= 1 - stereoPan * stereoPan;
            float halfVolume2D = volume2D * .5f;
            int actualSample = 0;
            for (int sample = 0; sample < listener.UpdateRate; ++sample) {
                float leftSample = leftSamples[sample], rightSample = rightSamples[sample],
                    leftGained = leftSample * leftVolume, rightGained = rightSample * rightVolume;
                for (int channel = 0; channel < Listener.Channels.Length; ++channel) {
                    if (Listener.Channels[channel].LFE) {
                        if (!listener.LFESeparation || LFE)
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

        /// <summary>Process the source and returns a mix to be added to the output.</summary>
        protected internal virtual float[] Collect() {
            // Preparations, clean environment
            int channels = Listener.Channels.Length;
            Array.Clear(rendered, 0, rendered.Length);

            // Update rate calculation
            int updateRate = listener.UpdateRate;
            int resampledNow = (int)(TimeSamples / resampleMult);

            // Render audio if not muted
            if (!Mute) {
                int clipChannels = Clip.Channels;

                // 3D renderer preprocessing
                if (SpatialBlend != 0)
                    if (listener.AudioQuality >= QualityModes.High && clipChannels != 1) { // Mono downmix above medium quality
                        Array.Clear(samples, 0, pitchedUpdateRate);
                        for (int channel = 0; channel < clipChannels; ++channel)
                            WaveformUtils.Mix(Rendered[channel], samples);
                        WaveformUtils.Gain(samples, 1f / clipChannels);
                    } else // First channel only otherwise
                        Buffer.BlockCopy(Rendered[0], 0, samples, 0, pitchedUpdateRate * sizeof(float));

                // 2D renderer
                if (SpatialBlend != 1) {
                    float volume2D = Volume * (1f - SpatialBlend);
                    // 1:1 mix for non-stereo sources
                    if (clipChannels != 2) {
                        samples = Resample.Adaptive(samples, updateRate, listener.AudioQuality);
                        WriteOutput(samples, rendered, volume2D, channels);
                    }

                    // Full side mix for stereo sources
                    else {
                        Buffer.BlockCopy(Rendered[0], 0, leftSamples, 0, pitchedUpdateRate * sizeof(float));
                        Buffer.BlockCopy(Rendered[1], 0, rightSamples, 0, pitchedUpdateRate * sizeof(float));
                        leftSamples = Resample.Adaptive(leftSamples, updateRate, listener.AudioQuality);
                        rightSamples = Resample.Adaptive(rightSamples, updateRate, listener.AudioQuality);
                        Stereo2DMix(volume2D);
                    }
                }

                // 3D mix, if the source is in range
                if (SpatialBlend != 0 && distance < listener.Range) {
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
                            // Find a bounding box
                            int bottomFrontLeft = -1,
                                bottomFrontRight = -1,
                                bottomRearLeft = -1,
                                bottomRearRight = -1,
                                topFrontLeft = -1,
                                topFrontRight = -1,
                                topRearLeft = -1,
                                topRearRight = -1;
                            // Closest layers on Y and Z axes
                            float closestTop = 80,
                                closestBottom = -65,
                                closestTF = 78,
                                closestTR = -78,
                                closestBF = 73,
                                closestBR = -3;
                            // Find closest horizontal layers
                            direction.Downscale(Listener.EnvironmentSize);
                            for (int channel = 0; channel < channels; ++channel) {
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
                                        AssignHorizontalLayer(channel, ref bottomFrontLeft, ref bottomFrontRight,
                                            ref bottomRearLeft, ref bottomRearRight, ref closestBF, ref closestBR, direction, channelPos);
                                    if (channelPos.y == closestTop) // Top layer
                                        AssignHorizontalLayer(channel, ref topFrontLeft, ref topFrontRight, ref topRearLeft, ref topRearRight,
                                            ref closestTF, ref closestTR, direction, channelPos);
                                }
                            }
                            FixIncompleteLayer(ref topFrontLeft, ref topFrontRight, ref topRearLeft, ref topRearRight); // Fix incomplete top layer

                            // When the bottom layer is completely empty (= the source is below all channels), copy the top layer
                            if (bottomFrontLeft == -1 && bottomFrontRight == -1 && bottomRearLeft == -1 && bottomRearRight == -1) {
                                bottomFrontLeft = topFrontLeft;
                                bottomFrontRight = topFrontRight;
                                bottomRearLeft = topRearLeft;
                                bottomRearRight = topRearRight;
                            }
                            // Fix incomplete bottom layer
                            else
                                FixIncompleteLayer(ref bottomFrontLeft, ref bottomFrontRight, ref bottomRearLeft, ref bottomRearRight);

                            // When the top layer is completely empty (= the source is above all channels), copy the bottom layer
                            if (topFrontLeft == -1 || topFrontRight == -1 || topRearLeft == -1 || topRearRight == -1) {
                                topFrontLeft = bottomFrontLeft;
                                topFrontRight = bottomFrontRight; topRearLeft = bottomRearLeft;
                                topRearRight = bottomRearRight;
                            }

                            // Spatial mix gain precalculation
                            float topVol, bottomVol;
                            if (topFrontLeft != bottomFrontLeft) { // Height ratio calculation
                                float bottomY = Listener.Channels[bottomFrontLeft].CubicalPos.y;
                                topVol = (direction.y - bottomY) / (Listener.Channels[topFrontLeft].CubicalPos.y - bottomY);
                                bottomVol = 1f - topVol;
                            } else
                                topVol = bottomVol = .5f;

                            // Length ratios
                            float BFVol = LengthRatio(bottomRearLeft, bottomFrontLeft, direction.z),
                                TFVol = LengthRatio(topRearLeft, topFrontLeft, direction.z);
                            // Width ratios
                            float BFRVol = WidthRatio(bottomFrontLeft, bottomFrontRight, direction.x),
                                BRRVol = WidthRatio(bottomRearLeft, bottomRearRight, direction.x),
                                TFRVol = WidthRatio(topFrontLeft, topFrontRight, direction.x),
                                TRRVol = WidthRatio(topRearLeft, topRearRight, direction.x),
                                innerVolume3D = volume3D;
                            if (Size != 0) {
                                BFVol = QMath.Lerp(BFVol, .5f, Size);
                                TFVol = QMath.Lerp(TFVol, .5f, Size);
                                BFRVol = QMath.Lerp(BFRVol, .5f, Size);
                                BRRVol = QMath.Lerp(BRRVol, .5f, Size);
                                TFRVol = QMath.Lerp(TFRVol, .5f, Size);
                                TRRVol = QMath.Lerp(TRRVol, .5f, Size);
                                innerVolume3D *= 1f - Size;
                                float extraChannelVolume = volume3D * Size / channels;
                                for (int channel = 0; channel < channels; ++channel)
                                    WriteOutput(samples, rendered, extraChannelVolume, channel, channels);
                            }
                            // Remaining length ratios
                            float BRVol = 1f - BFVol,
                                TRVol = 1f - TFVol;

                            // Spatial mix gain finalization
                            bottomVol *= innerVolume3D;
                            topVol *= innerVolume3D;
                            BFVol *= bottomVol;
                            BRVol *= bottomVol;
                            TFVol *= topVol;
                            TRVol *= topVol;
                            WriteOutput(samples, rendered, BFVol * (1f - BFRVol), bottomFrontLeft, channels);
                            WriteOutput(samples, rendered, BFVol * BFRVol, bottomFrontRight, channels);
                            WriteOutput(samples, rendered, BRVol * (1f - BRRVol), bottomRearLeft, channels);
                            WriteOutput(samples, rendered, BRVol * BRRVol, bottomRearRight, channels);
                            WriteOutput(samples, rendered, TFVol * (1f - TFRVol), topFrontLeft, channels);
                            WriteOutput(samples, rendered, TFVol * TFRVol, topFrontRight, channels);
                            WriteOutput(samples, rendered, TRVol * (1f - TRRVol), topRearLeft, channels);
                            WriteOutput(samples, rendered, TRVol * TRRVol, topRearRight, channels);
                        }
                        // LFE mix
                        if (!listener.LFESeparation || LFE)
                            for (int channel = 0; channel < channels; ++channel)
                                if (Listener.Channels[channel].LFE)
                                    WriteOutput(samples, rendered, volume3D, channel, channels);
                    }

                    // ------------------------------------------------------------------
                    // Directional/distance-based engine for asymmetrical layouts
                    // ------------------------------------------------------------------
                    else {
                        // Angle match calculations
                        float[] angleMatches;
                        if (listener.AudioQuality >= QualityModes.High) {
                            if (Listener.EnvironmentType == Environments.Theatre)
                                angleMatches = CalculateAngleMatches(channels, direction, PowTo16);
                            else
                                angleMatches = CalculateAngleMatches(channels, direction, PowTo8);
                        } else if (Listener.EnvironmentType == Environments.Theatre)
                            angleMatches = LinearizeAngleMatches(channels, direction, PowTo16);
                        else
                            angleMatches = LinearizeAngleMatches(channels, direction, PowTo8);

                        // Object size extension
                        if (Size != 0) {
                            float maxAngleMatch = angleMatches[0];
                            for (int channel = 1; channel < channels; ++channel)
                                if (maxAngleMatch < angleMatches[channel])
                                    maxAngleMatch = angleMatches[channel];
                            for (int channel = 0; channel < channels; ++channel)
                                angleMatches[channel] = QMath.Lerp(angleMatches[channel], maxAngleMatch, Size);
                        }
                        // Only use the closest 3 speakers on non-Perfect qualities or in Theatre mode
                        if (listener.AudioQuality != QualityModes.Perfect || Listener.EnvironmentType == Environments.Theatre) {
                            float top0 = 0, top1 = 0, top2 = 0;
                            for (int channel = 0; channel < channels; ++channel) {
                                if (!Listener.Channels[channel].LFE) {
                                    float match = angleMatches[channel];
                                    if (top0 < match) {
                                        top2 = top1;
                                        top1 = top0;
                                        top0 = match;
                                    } else if (top1 < match) {
                                        top2 = top1;
                                        top1 = match;
                                    } else if (top2 < match)
                                        top2 = match;
                                }
                            }
                            for (int channel = 0; channel < channels; ++channel)
                                if (!Listener.Channels[channel].LFE &&
                                    angleMatches[channel] != top0 && angleMatches[channel] != top1 && angleMatches[channel] != top2)
                                    angleMatches[channel] = 0;
                        }
                        // Place in sphere, write data to output channels
                        float totalAngleMatch = 0;
                        for (int channel = 0; channel < channels; ++channel)
                            totalAngleMatch += angleMatches[channel];
                        float volume3D = Volume * rolloffDistance * SpatialBlend / totalAngleMatch;
                        for (int channel = 0; channel < channels; ++channel) {
                            if (Listener.Channels[channel].LFE) {
                                if (!listener.LFESeparation || LFE)
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