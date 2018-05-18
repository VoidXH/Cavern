using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern {
    public partial class AudioSource3D : MonoBehaviour {
        /// <summary>Audio output writer delegate.</summary>
        /// <param name="Samples"></param>
        /// <param name="Target"></param>
        /// <param name="ChannelLength"></param>
        /// <param name="Gain"></param>
        /// <param name="Channel"></param>
        /// <param name="Channels"></param>
        internal delegate void OutputFunc(float[] Samples, float[] Target, int ChannelLength, float Gain, int Channel, int Channels);

        /// <summary>The audio output function to be used.</summary>
        internal static OutputFunc UsedOutputFunc;

        /// <summary>Output samples to a multichannel array.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteOutput(float[] Samples, float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
            for (int From = 0, To = Channel; From < ChannelLength; ++From, To += Channels)
                Target[To] += Samples[From] * Gain;
        }

        /// <summary>Output samples to a multichannel array with approximated constant power. The error margin is 15%.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteOutputApproxCP(float[] Samples, float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
            WriteOutput(Samples, Target, ChannelLength, Mathf.Sqrt(Gain), Channel, Channels);
        }

        /// <summary>Output samples to a multichannel array with constant power.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteOutputCP(float[] Samples, float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
            WriteOutput(Samples, Target, ChannelLength, Mathf.Sin(Gain * CavernUtilities.halfPi), Channel, Channels);
        }

        /// <summary>Output samples to a multichannel array, while trying to fix standing waves.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteFixedOutput(float[] Samples, float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
            int FirstPassLength = ChannelLength;
            float OldMax = 0, NewMax = 0, AbsSample;
            fixed (float* From = Samples, To = Target) {
                for (int FromSample = 0, ToSample = Channel; FromSample < ChannelLength; ++FromSample, ToSample += Channels) {
                    float* TargetSample = To + ToSample;
                    AbsSample = CavernUtilities.Abs(*TargetSample);
                    if (OldMax < AbsSample)
                        OldMax = AbsSample;
                    *TargetSample += From[FromSample] * Gain;
                    AbsSample = CavernUtilities.Abs(*TargetSample);
                    if (NewMax < AbsSample)
                        NewMax = AbsSample;
                }
            }
            if (NewMax < OldMax)
                WriteOutput(Samples, Target, ChannelLength, Gain * -2, Channel, Channels);
        }

        /// <summary>Output samples to a multichannel array with approximated constant power, while trying to fix standing waves.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteFixedOutputApproxCP(float[] Samples, float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
            WriteFixedOutput(Samples, Target, ChannelLength, Mathf.Sqrt(Gain), Channel, Channels);
        }

        /// <summary>Output samples to a multichannel array with constant power, while trying to fix standing waves.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteFixedOutputCP(float[] Samples, float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
            WriteFixedOutput(Samples, Target, ChannelLength, Mathf.Sin(Gain * CavernUtilities.halfPi), Channel, Channels);
        }
    }
}