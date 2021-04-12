using System;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern {
    public partial class Source {
        // ------------------------------------------------------------------
        // Helpers for the asymmetric renderer
        // ------------------------------------------------------------------
        /// <summary>Angle match value modifier.</summary>
        /// <param name="Matching">Old angle match</param>
        internal delegate float MatchModifierFunc(float Matching);

        /// <summary>x to the power of 8.</summary>
        /// <returns>x^8 the fastest way possible</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float PowTo8(float x) {
            x *= x;
            x *= x;
            return x * x;
        }

        /// <summary>x to the power of 16.</summary>
        /// <returns>x^16 the fastest way possible</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float PowTo16(float x) {
            x *= x;
            x *= x;
            x *= x;
            return x * x;
        }

        /// <summary>Angle match calculations.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float[] CalculateAngleMatches(int channels, Vector direction, MatchModifierFunc matchModifier) {
            float[] angleMatches = new float[channels];
            float dirMagnitudeRecip = 1f / (direction.Magnitude + .0001f);
            for (int channel = 0; channel < channels; ++channel) {
                Channel currentChannel = Listener.Channels[channel];
                if (!currentChannel.LFE)
                    angleMatches[channel] =
                        matchModifier((float)(Math.PI - Math.Acos(direction.Dot(currentChannel.SphericalPos) * dirMagnitudeRecip)));
            }
            return angleMatches;
        }

        /// <summary>Linearized <see cref="CalculateAngleMatches(int, Vector, MatchModifierFunc)"/>:
        /// pi / 2 - pi / 2 * x, angle match: pi - (lin acos) = pi / 2 + pi / 2 * x.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float[] LinearizeAngleMatches(int channels, Vector direction, MatchModifierFunc matchModifier) {
            float[] angleMatches = new float[channels];
            float dirMagnitudeRecip = 1f / (direction.Magnitude + .0001f);
            for (int channel = 0; channel < channels; ++channel) {
                Channel currentChannel = Listener.Channels[channel];
                if (!currentChannel.LFE)
                    angleMatches[channel] =
                        matchModifier(1.570796326f + 1.570796326f * direction.Dot(currentChannel.SphericalPos) * dirMagnitudeRecip);
            }
            return angleMatches;
        }
    }
}