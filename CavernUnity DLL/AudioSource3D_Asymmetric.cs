using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern {
    public partial class AudioSource3D : MonoBehaviour {
        // ------------------------------------------------------------------
        // Helpers for the asymmetric renderer
        // ------------------------------------------------------------------
        /// <summary>x to the power of 8.</summary>
        /// <param name="x">Input number</param>
        /// <returns>x^8 the fastest way possible</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float PowTo8(float x) { x = x * x; x = x * x; return x * x; }

        /// <summary>x to the power of 16.</summary>
        /// <param name="x">Input number</param>
        /// <returns>x^16 the fastest way possible</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float PowTo16(float x) { x = x * x; x = x * x; x = x * x; return x * x; }

        /// <summary>Angle match calculations.</summary>
        /// <param name="Channels">Output layout channel count</param>
        /// <param name="Direction">The source's direction from the <see cref="AudioListener3D"/></param>
        /// <param name="MatchModifier">Modifier function of angle match values</param>
        /// <returns>Angle matches for each channel</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float[] CalculateAngleMatches(int Channels, Vector3 Direction, Func<float, float> MatchModifier) {
            float[] AngleMatches = new float[Channels];
            float DirMagnitudeRecip = 1f / (Direction.magnitude + .0001f);
            for (int Channel = 0; Channel < Channels; ++Channel) {
                if (!AudioListener3D.Channels[Channel].LFE) {
                    float Multiplication = Direction.x * AudioListener3D.ChannelDirections[Channel].x + Direction.y * AudioListener3D.ChannelDirections[Channel].y +
                        Direction.z * AudioListener3D.ChannelDirections[Channel].z;
                    AngleMatches[Channel] = MatchModifier((float)(3.1415926535897932384626433832795 -
                        Math.Acos(Multiplication * AudioListener3D.ChannelDistRecips[Channel] * DirMagnitudeRecip)));
                }
            }
            return AngleMatches;
        }

        /// <summary>Linearized <see cref="CalculateAngleMatches(int, Vector3, Func{float, float})"/>:
        /// pi / 2 - pi / 2 * x, angle match: pi - (lin acos) = pi / 2 + pi / 2 * x.</summary>
        /// <param name="Channels">Output layout channel count</param>
        /// <param name="Direction">The source's direction from the <see cref="AudioListener3D"/></param>
        /// <param name="MatchModifier">Modifier function of angle match values</param>
        /// <returns>Angle matches for each channel</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float[] LinearizeAngleMatches(int Channels, Vector3 Direction, Func<float, float> MatchModifier) {
            float[] AngleMatches = new float[Channels];
            float DirMagnitudeRecip = 1f / (Direction.magnitude + .0001f);
            for (int Channel = 0; Channel < Channels; ++Channel) {
                if (!AudioListener3D.Channels[Channel].LFE) {
                    float Multiplication = Direction.x * AudioListener3D.ChannelDirections[Channel].x + Direction.y * AudioListener3D.ChannelDirections[Channel].y +
                        Direction.z * AudioListener3D.ChannelDirections[Channel].z;
                    AngleMatches[Channel] = MatchModifier(1.570796326f + 1.570796326f * (Multiplication * AudioListener3D.ChannelDistRecips[Channel] * DirMagnitudeRecip));
                }
            }
            return AngleMatches;
        }
    }
}
