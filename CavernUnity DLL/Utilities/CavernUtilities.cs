using Cavern.Utilities;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern {
    /// <summary>Useful functions used in multiple classes.</summary>
    public static class CavernUtilities {
        /// <summary>Downmix audio for a lesser channel count with limited knowledge of the target system's channel locations.</summary>
        public static void Downmix(float[] from, int fromChannels, float[] to, int toChannels) {
            int samplesPerChannel = to.Length / toChannels;
            for (int channel = 0, unityChannel = 0; channel < fromChannels; ++channel, unityChannel = channel % toChannels) {
                if (toChannels > 4 || (Listener.Channels[channel].Y != 0 && !Listener.Channels[channel].LFE))
                    for (int sample = 0; sample < samplesPerChannel; ++sample)
                        to[sample * toChannels + unityChannel] += from[sample * fromChannels + channel];
                else {
                    for (int sample = 0; sample < samplesPerChannel; ++sample) {
                        int leftOut = sample * toChannels;
                        float copySample = from[sample * fromChannels + channel];
                        to[leftOut] += copySample;
                        to[leftOut + 1] += copySample;
                    }
                }
            }
        }

        /// <summary>Clamped linear vector interpolation</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 FastLerp(Vector3 from, Vector3 to, float t) {
            if (t >= 1)
                return to;
            if (t <= 0)
                return from;
            return new Vector3((to.x - from.x) * t + from.x, (to.y - from.y) * t + from.y, (to.z - from.z) * t + from.z);
        }

        /// <summary>Get the peak amplitude of a single-channel array.</summary>
        /// <param name="target">Array reference</param>
        /// <returns>Peak amplitude in the array in decibels</returns>
        public static float GetPeak(float[] target) {
            float max = Math.Abs(target[0]), absSample;
            for (int sample = 1; sample < target.Length; ++sample) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample)
                    max = absSample;
            }
            return max != 0 ? (20 * Mathf.Log10(max)) : -300;
        }

        /// <summary>Get the peak amplitude of a given channel in a multichannel array.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="samples">Samples per channel</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        /// <returns>Maximum absolute value in the array</returns>
        internal static float GetPeak(float[] target, int samples, int channel, int channels) {
            float max = 0, absSample;
            for (int sample = channel, end = samples * channels; sample < end; sample += channels) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample)
                    max = absSample;
            }
            return max;
        }

        /// <summary>Compute the base 2 logarithm of a number faster than a generic Log function.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Log2(int value) {
            int log = -1;
            while (value > 255) {
                log += 8;
                value >>= 8;
            }
            while (value != 0) {
                ++log;
                value >>= 1;
            }
            return log;
        }

        /// <summary>Converts a signal strength (ref = 1) to dB.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float SignalToDb(float amplitude) => 20 * Mathf.Log10(amplitude);

        /// <summary>Converts a dB value (ref = 0) to signal strength.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float DbToSignal(float amplitude) => Mathf.Pow(10, 1/20f * amplitude);

        /// <summary>Converts a Unity vector to a Cavern vector.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector VectorMatch(Vector3 source) => new Vector(source.x, source.y, source.z);

        /// <summary>Converts a Cavern vector to a Unity vector.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 VectorMatch(Vector source) => new Vector3(source.x, source.y, source.z);

        /// <summary>Checks if a Cavern and Unity vector are equal.</summary>
        public static bool VectorCompare(Vector cavernVector, Vector3 unityVector) =>
            cavernVector.x == unityVector.x && cavernVector.y == unityVector.y && cavernVector.z == unityVector.z;
    }
}