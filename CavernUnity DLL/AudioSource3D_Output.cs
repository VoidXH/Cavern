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
        internal static unsafe void WriteOutput(ref float[] Samples, ref float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
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
        internal static void WriteFixedOutput(ref float[] Samples, ref float[] Target, int ChannelLength, float Gain, int Channel, int Channels) {
            float OldMax = CavernUtilities.GetPeak(ref Target, ChannelLength, Channel, Channels);
            WriteOutput(ref Samples, ref Target, ChannelLength, Gain, Channel, Channels);
            float NewMax = CavernUtilities.GetPeak(ref Target, ChannelLength, Channel, Channels);
            if (NewMax < OldMax)
                WriteOutput(ref Samples, ref Target, ChannelLength, Gain * -2, Channel, Channels);
        }

        /// <summary>Method for outputting audio to a single channel.</summary>
        /// <param name="Samples">Samples</param>
        /// <param name="Target">Multichannel array (destination)</param>
        /// <param name="ChannelLength">Sample count for a single channel</param>
        /// <param name="Gain">Gain</param>
        /// <param name="Channel">Channel</param>
        /// <param name="Channels">Channel count</param>
        internal delegate void OutputFunc(ref float[] Samples, ref float[] Target, int ChannelLength, float Gain, int Channel, int Channels);

        /// <summary>The audio output function to be used.</summary>
        internal static OutputFunc UsedOutputFunc;
    }
}