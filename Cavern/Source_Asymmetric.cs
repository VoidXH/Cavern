using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Cavern {
    public partial class Source {
        // ------------------------------------------------------------------
        // Helpers for the asymmetric renderer
        // ------------------------------------------------------------------
        /// <summary>
        /// Angle match value modifier.
        /// </summary>
        /// <param name="Matching">Old angle match</param>
        internal delegate float MatchModifierFunc(float Matching);

        /// <summary>
        /// Angle match calculations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float[] CalculateAngleMatches(int channels, Vector3 direction) {
            float[] angleMatches = new float[channels];
            float dirMagnitudeRecip = 1f / (direction.Length() + .0001f);
            for (int channel = 0; channel < channels; ++channel) {
                Channel currentChannel = Listener.Channels[channel];
                if (!currentChannel.LFE) {
                    float sample = MathF.PI - MathF.Acos(Vector3.Dot(direction, currentChannel.SphericalPos) * dirMagnitudeRecip);
                    sample *= sample;
                    sample *= sample;
                    angleMatches[channel] = sample * sample; // Angle match modifier function is x^8
                }
            }
            if (Listener.EnvironmentType == Environments.Theatre) {
                for (int channel = 0; channel < channels; ++channel) {
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
        internal static float[] LinearizeAngleMatches(int channels, Vector3 direction) {
            float[] angleMatches = new float[channels];
            float dirMagnitudeRecip = 1f / (direction.Length() + .0001f);
            for (int channel = 0; channel < channels; ++channel) {
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
                for (int channel = 0; channel < channels; ++channel) {
                    angleMatches[channel] *= angleMatches[channel]; // Theatre angle match modifier function is x^16
                }
            }
            return angleMatches;
        }
    }
}