using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>Useful functions used in multiple classes.</summary>
    public static class Utils {
        /// <summary>Reference sound velocity in m/s.</summary>
        public const float SpeedOfSound = 340.29f;
        /// <summary>Converts degrees to radians. = pi / 180.</summary>
        internal const float Deg2Rad = .01745329251f;
        /// <summary>sqrt(2) / 2 = 1 / sqrt(2)</summary>
        internal const float Sqrt2p2 = .7071067811f;

        /// <summary>Cached version name.</summary>
        static string info;
        /// <summary>Version and creator information.</summary>
        public static string Info => info ?? (info = "Cavern v" +
            FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion + " by VoidX (www.cavern.cf)");

        /// <summary>For given angles (in radians) it returns a vector for that position on a sphere with the radius of 1.</summary>
        public static Vector PlaceInSphere(Vector angles) {
            float xRad = angles.x * Deg2Rad, yRad = angles.y * Deg2Rad, sinX = (float)Math.Sin(xRad), cosX = (float)Math.Cos(xRad),
                sinY = (float)Math.Sin(yRad), cosY = (float)Math.Cos(yRad);
            return new Vector(sinY * cosX, -sinX, cosY * cosX);
        }

        /// <summary>For given angles (in radians) it returns a vector for that position on a cube with the side length of 2.</summary>
        public static Vector PlaceInCube(Vector angles) {
            float xRad = angles.x * Deg2Rad, yRad = angles.y * Deg2Rad, sinX = (float)Math.Sin(xRad), cosX = (float)Math.Cos(xRad),
                sinY = (float)Math.Sin(yRad), cosY = (float)Math.Cos(yRad);
            if (Math.Abs(sinY) > Math.Abs(cosY))
                sinY = Math.Sign(sinY) * Sqrt2p2;
            else
                cosY = Math.Sign(cosY) * Sqrt2p2;
            sinY /= Sqrt2p2;
            cosY /= Sqrt2p2;
            if (Math.Abs(sinX) >= Sqrt2p2) {
                sinX = Math.Sign(sinX) * Sqrt2p2;
                cosX /= Sqrt2p2;
                sinY *= cosX;
                cosY *= cosX;
            }
            sinX /= Sqrt2p2;
            return new Vector(sinY, -sinX, cosY);
        }

        /// <summary>Keeps a value in the given array, if it's smaller than any of its contents.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="value">Value to insert</param>
        internal static void BottomlistHandler(float[] target, float value) {
            int replace = -1;
            for (int record = 0; record < target.Length; ++record)
                if (target[record] > value)
                    replace = replace == -1 ? record : (target[record] > target[replace] ? record : replace);
            if (replace != -1)
                target[replace] = value;
        }

        /// <summary>
        /// Clamp a float between limits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max) {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        /// <summary>
        /// Clamp an int between limits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max) {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        /// <summary>Unclamped linear interpolation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float from, float to, float t) => (to - from) * t + from;

        /// <summary>Compute the base 2 logarithm of a number faster than a generic Log function.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(int value) {
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

        /// <summary>Multiplies all values in an array.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="value">Multiplier</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Gain(float[] target, float value) {
            for (int i = 0; i < target.Length; ++i)
                target[i] *= value;
        }

        /// <summary>Set gain for a channel in a multichannel array.</summary>
        /// <param name="target">Sample reference</param>
        /// <param name="gain">Gain</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Gain(float[] target, float gain, int channel, int channels) {
            for (int sample = channel; sample < target.Length; sample += channels)
                target[sample] *= gain;
        }

        /// <summary>Mix a track to a stream.</summary>
        /// <param name="from">Track</param>
        /// <param name="to">Stream</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Mix(float[] from, float[] to) {
            for (int i = 0; i < from.Length; ++i)
                to[i] += from[i];
        }

        /// <summary>Normalize an array of samples.</summary>
        /// <param name="target">Samples to normalize</param>
        /// <param name="decayFactor">Gain increment per frame, should be decay rate * update rate / sample rate</param>
        /// <param name="lastGain">Last normalizer gain (a reserved float with a default of 1 to always pass to this function)</param>
        /// <param name="limiterOnly">Don't go over 0 dB gain</param>
        public static void Normalize(ref float[] target, float decayFactor, ref float lastGain, bool limiterOnly) {
            float max = Math.Abs(target[0]), absSample;
            for (int sample = 1; sample < target.Length; ++sample) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample)
                    max = absSample;
            }
            if (max * lastGain > 1) // Attack
                lastGain = .9f / max;
            Gain(target, lastGain); // Normalize last samples
            // Decay
            lastGain += decayFactor;
            if (limiterOnly && lastGain > 1)
                lastGain = 1;
        }
    }
}