using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern {
    public partial class AudioSource3D : MonoBehaviour {
        /// <summary>Output samples to a multichannel array.</summary>
        /// <param name="Samples">Samples</param>
        /// <param name="Target">Multichannel array (destination)</param>
        /// <param name="ChannelLength">Sample count for a single channel</param>
        /// <param name="Gain">Gain</param>
        /// <param name="Channel">Channel</param>
        /// <param name="Channels">Channel count</param>
        internal static unsafe void WriteOutput(float[] Samples, float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
            fixed (float* FromPtr = Samples, ToPtr = Target) {
                float* FromArr = FromPtr, ToArr = ToPtr + Channel;
                do {
                    *ToArr += *FromArr++ * Gain;
                    ToArr += Channels;
                } while (--ChannelLength != 0);
            }
        }

        /// <summary>Output samples to a multichannel array, while trying to fix standing waves.</summary>
        /// <param name="Samples">Samples</param>
        /// <param name="Target">Multichannel array (destination)</param>
        /// <param name="ChannelLength">Sample count for a single channel</param>
        /// <param name="Gain">Gain</param>
        /// <param name="Channel">Target channel</param>
        /// <param name="Channels">Channel count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteFixedOutput(float[] Samples, float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
            int FirstPassLength = ChannelLength;
            float OldMax = 0, NewMax = 0, AbsSample;
            fixed (float* FromPtr = Samples, ToPtr = Target) {
                float* FromArr = FromPtr, ToArr = ToPtr + Channel;
                do {
                    AbsSample = CavernUtilities.Abs(*ToArr);
                    if (OldMax < AbsSample)
                        OldMax = AbsSample;
                    *ToArr += *FromArr++ * Gain;
                    AbsSample = CavernUtilities.Abs(*ToArr);
                    if (NewMax < AbsSample)
                        NewMax = AbsSample;
                    ToArr += Channels;
                } while (--FirstPassLength != 0);
            }
            if (NewMax < OldMax)
                WriteOutput(Samples, Target, ChannelLength, Gain * -2, Channel, Channels);
        }

        /// <summary>The audio output function to be used.</summary>
        internal static Action<float[], float[], int, float, int, int> UsedOutputFunc;
    }
}