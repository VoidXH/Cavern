using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern.Rendering {
    /// <summary>
    /// Mixes <see cref="Source"/>s to <see cref="Listener.Channels"/> based on which channels are boundary to the direction vector.
    /// </summary>
    public class DirectionalRenderer : SourceRenderer {
        /// <summary>
        /// Angle match calculations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float[] CalculateAngleMatches(int channels, Vector3 direction) {
            float[] angleMatches = new float[channels];
            float dirMagnitudeRecip = 1f / (direction.Length() + .0001f);
            for (int channel = 0; channel < channels; channel++) {
                Channel currentChannel = Listener.Channels[channel];
                if (!currentChannel.LFE) {
                    float sample = MathF.PI - MathF.Acos(Vector3.Dot(direction, currentChannel.SphericalPos) * dirMagnitudeRecip);
                    sample *= sample;
                    sample *= sample;
                    angleMatches[channel] = sample * sample; // Angle match modifier function is x^8
                }
            }
            if (Listener.EnvironmentType == Environments.Theatre) {
                for (int channel = 0; channel < channels; channel++) {
                    angleMatches[channel] *= angleMatches[channel]; // Theatre angle match modifier function is x^16
                }
            }
            return angleMatches;
        }

        /// <summary>
        /// Linearized <see cref="CalculateAngleMatches(int, Vector3)"/>:
        /// pi / 2 - pi / 2 * x, angle match: pi - (lin acos) = pi / 2 + pi / 2 * x.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float[] LinearizeAngleMatches(int channels, Vector3 direction) {
            float[] angleMatches = new float[channels];
            float dirMagnitudeRecip = 1f / (direction.Length() + .0001f);
            for (int channel = 0; channel < channels; channel++) {
                Channel currentChannel = Listener.Channels[channel];
                if (!currentChannel.LFE) {
                    float sample = 1.570796326f +
                        1.570796326f * Vector3.Dot(direction, currentChannel.SphericalPos) * dirMagnitudeRecip;
                    sample *= sample;
                    sample *= sample;
                    angleMatches[channel] = sample * sample; // Angle match modifier function is x^8
                }
            }
            if (Listener.EnvironmentType == Environments.Theatre) {
                for (int channel = 0; channel < channels; channel++) {
                    angleMatches[channel] *= angleMatches[channel]; // Theatre angle match modifier function is x^16
                }
            }
            return angleMatches;
        }

        /// <inheritdoc/>
        public override void Render(Listener listener, Source source, Vector3 direction, float[] samples, float[] rendered, float gain) {
            if (source.LFE || !listener.LFESeparation) {
                MixToLFE(samples, rendered, gain);
                if (source.LFE) {
                    return;
                }
            }

            // Angle match calculations
            float[] angleMatches;
            Channel[] channels = Listener.Channels;
            if (listener.AudioQuality >= QualityModes.High) {
                angleMatches = CalculateAngleMatches(channels.Length, direction);
            } else {
                angleMatches = LinearizeAngleMatches(channels.Length, direction);
            }

            // Object size extension
            if (source.Size != 0) {
                float maxAngleMatch = angleMatches[0];
                for (int channel = 1; channel < channels.Length; channel++) {
                    if (maxAngleMatch < angleMatches[channel]) {
                        maxAngleMatch = angleMatches[channel];
                    }
                }
                for (int channel = 0; channel < channels.Length; channel++) {
                    angleMatches[channel] = QMath.Lerp(angleMatches[channel], maxAngleMatch, source.Size);
                }
            }

            // Only use the closest 3 speakers on non-Perfect qualities or in Theatre mode
            if (listener.AudioQuality != QualityModes.Perfect || Listener.EnvironmentType == Environments.Theatre) {
                float top0 = 0, top1 = 0, top2 = 0;
                for (int channel = 0; channel < channels.Length; channel++) {
                    if (!channels[channel].LFE) {
                        float match = angleMatches[channel];
                        if (top0 < match) {
                            top2 = top1;
                            top1 = top0;
                            top0 = match;
                        } else if (top1 < match) {
                            top2 = top1;
                            top1 = match;
                        } else if (top2 < match) {
                            top2 = match;
                        }
                    }
                }
                for (int channel = 0; channel < channels.Length; channel++) {
                    if (!channels[channel].LFE && angleMatches[channel] != top0 && angleMatches[channel] != top1 && angleMatches[channel] != top2) {
                        angleMatches[channel] = 0;
                    }
                }
            }
            float totalAngleMatch = 0;
            for (int channel = 0; channel < channels.Length; channel++) {
                totalAngleMatch += angleMatches[channel] * angleMatches[channel];
            }
            totalAngleMatch = MathF.Sqrt(totalAngleMatch);

            // Place in sphere, write data to output channels
            float gain3D = gain / totalAngleMatch;
            for (int channel = 0; channel < channels.Length; channel++) {
                if (!channels[channel].LFE && angleMatches[channel] != 0) {
                    WaveformUtils.Mix(samples, rendered, channel, channels.Length, gain3D * angleMatches[channel]);
                }
            }
        }
    }
}
