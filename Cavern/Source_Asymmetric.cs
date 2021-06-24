using System;
using System.Numerics;
using System.Runtime.CompilerServices;

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
        internal static float[] CalculateAngleMatches(int channels, Vector3 direction, MatchModifierFunc matchModifier) {
            float[] angleMatches = new float[channels];
            float dirMagnitudeRecip = 1f / (direction.Length() + .0001f);
            for (int channel = 0; channel < channels; ++channel) {
                Channel currentChannel = Listener.Channels[channel];
                if (!currentChannel.LFE)
                    angleMatches[channel] =
                        matchModifier((float)(Math.PI - Math.Acos(Vector3.Dot(direction, currentChannel.SphericalPos) * dirMagnitudeRecip)));
            }
            return angleMatches;
        }

        /// <summary>Linearized <see cref="CalculateAngleMatches(int, Vector3, MatchModifierFunc)"/>:
        /// pi / 2 - pi / 2 * x, angle match: pi - (lin acos) = pi / 2 + pi / 2 * x.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float[] LinearizeAngleMatches(int channels, Vector3 direction, MatchModifierFunc matchModifier) {
            float[] angleMatches = new float[channels];
            float dirMagnitudeRecip = 1f / (direction.Length() + .0001f);
            for (int channel = 0; channel < channels; ++channel) {
                Channel currentChannel = Listener.Channels[channel];
                if (!currentChannel.LFE)
                    angleMatches[channel] =
                        matchModifier(1.570796326f + 1.570796326f * Vector3.Dot(direction, currentChannel.SphericalPos) * dirMagnitudeRecip);
            }
            return angleMatches;
        }
    }
}