using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern {
    /// <summary>Useful functions used in multiple classes.</summary>
    public static class CavernUtilities {
        /// <summary>Cached version name.</summary>
        static string _Info;
        /// <summary>Version and creator information.</summary>
        public static string Info {
            get {
                if (_Info == null)
                    _Info = "Cavern v" + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion +
                        " by VoidX (www.voidx.tk)";
                return _Info;
            }
        }

        /// <summary>pi / 2</summary>
        internal const float halfPi = Mathf.PI * .5f;
        /// <summary>sqrt(2) / 2</summary>
        internal const float sqrt2p2 = .7071067811f;
        /// <summary>sqrt(2) / -2</summary>
        internal const float sqrt2pm2 = -.7071067811f;

        /// <summary>For given angles (in radian) it returns a vector for that position on a sphere with the radius of 1.</summary>
        public static Vector3 PlaceInSphere(Vector3 Angles) {
            float XRad = Angles.x * Mathf.Deg2Rad, YRad = Angles.y * Mathf.Deg2Rad, SinX = (float)Math.Sin(XRad), CosX = (float)Math.Cos(XRad),
                SinY = (float)Math.Sin(YRad), CosY = (float)Math.Cos(YRad);
            return new Vector3(SinY * CosX, -SinX, CosY * CosX);
        }

        /// <summary>For given angles (in radian) it returns a vector for that position on a cube with the side length of 2.</summary>
        public static Vector3 PlaceInCube(Vector3 Angles) {
            float XRad = Angles.x * Mathf.Deg2Rad, YRad = Angles.y * Mathf.Deg2Rad, SinX = (float)Math.Sin(XRad), CosX = (float)Math.Cos(XRad),
                SinY = (float)Math.Sin(YRad), CosY = (float)Math.Cos(YRad);
            if (Abs(SinY) > Abs(CosY)) {
                SinY = SinY > 0 ? sqrt2p2 : sqrt2pm2;
            } else
                CosY = CosY > 0 ? sqrt2p2 : sqrt2pm2;
            SinY /= sqrt2p2;
            CosY /= sqrt2p2;
            if (Abs(SinX) >= sqrt2p2) {
                SinX = SinX > 0 ? sqrt2p2 : sqrt2pm2;
                CosX /= sqrt2p2;
                SinY *= CosX;
                CosY *= CosX;
            }
            SinX /= sqrt2p2;
            return new Vector3(SinY, -SinX, CosY);
        }

        /// <summary>Quickly checks if a value is in an array.</summary>
        /// <param name="Target">Array reference</param>
        /// <param name="Count">Array length</param>
        /// <param name="Value">Value to check</param>
        /// <returns>If an array contains the value</returns>
        internal static unsafe bool ArrayContains(ref float[] Target, int Count, float Value) {
            for (int Entry = 0; Entry < Count; ++Entry)
                if (Target[Entry] == Value)
                    return true;
            return false;
        }

        /// <summary>Quickly gets the maximum value from an array.</summary>
        /// <param name="Target">Array reference</param>
        /// <param name="Count">Array length</param>
        internal static unsafe float ArrayMaximum(float[] Target, int Count) {
            float Max = float.NegativeInfinity;
            fixed (float* Pointer = Target) {
                for (int Entry = 0; Entry < Count; ++Entry) {
                    float* Pos = Pointer + Entry;
                    if (Max < *Pos)
                        Max = *Pos;
                }
            }
            return Max;
        }

        /// <summary>Keeps a value in the given array, if it's smaller than any of its contents.</summary>
        /// <param name="in0">Array reference</param>
        /// <param name="in1">Array length</param>
        /// <param name="in2">Value to insert</param>
        internal static void BottomlistHandler(ref float[] in0, int in1, float in2) {
            int Replace = -1;
            for (int Record = 0; Record < in1; ++Record)
                if (in0[Record] > in2)
                    Replace = Replace == -1 ? Record : (in0[Record] > in0[Replace] ? Record : Replace);
            if (Replace != -1)
                in0[Replace] = in2;
        }

        /// <summary>Unclamped linear interpolation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float FastLerp(float a, float b, float t) {
            return (b - a) * t + a;
        }

        /// <summary>Clamped linear vector interpolation</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 FastLerp(Vector3 in0, Vector3 in1, float in2) {
            if (in2 >= 1)
                return in1;
            if (in2 <= 0)
                return in0;
            return new Vector3((in1.x - in0.x) * in2 + in0.x, (in1.y - in0.y) * in2 + in0.y, (in1.z - in0.z) * in2 + in0.z);
        }

        /// <summary>Get the peak amplitude of a single-channel array.</summary>
        /// <param name="Target">Array reference</param>
        /// <param name="Samples">Sample count</param>
        /// <returns>Maximum absolute value in the array</returns>
        public static unsafe float GetPeak(ref float[] Target, int Samples) {
            float Max = 0, AbsSample;
            int Absolute;
            fixed (float* Pointer = Target) {
                for (int Sample = 0; Sample < Samples; ++Sample) {
                    Absolute = (*(int*)(Pointer + Sample)) & 0x7fffffff;
                    AbsSample = (*(float*)&Absolute);
                    if (Max < AbsSample)
                        Max = AbsSample;
                }
            }
            return Max != 0 ? (20 * Mathf.Log10(Max)) : -300;
        }

        /// <summary>Get the peak amplitude of a given channel in a multichannel array.</summary>
        /// <param name="Target">Array reference</param>
        /// <param name="Samples">Samples per channel</param>
        /// <param name="Channel">Target channel</param>
        /// <param name="Channels">Channel count</param>
        /// <returns>Maximum absolute value in the array</returns>
        internal static unsafe float GetPeak(ref float[] Target, int Samples, int Channel, int Channels) {
            float Max = 0, AbsSample;
            int Absolute;
            fixed (float* Pointer = Target) {
                for (int Sample = Channel, End = Samples * Channels; Sample < End; Sample += Channels) {
                    Absolute = (*(int*)(Pointer + Sample)) & 0x7fffffff;
                    AbsSample = (*(float*)&Absolute);
                    if (Max < AbsSample)
                        Max = AbsSample;
                }
            }
            return Max;
        }

        /// <summary>Vector scaling by each axis.</summary>
        /// <param name="Target">Input vector</param>
        /// <param name="Scale">Scale</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 VectorScale(Vector3 Target, Vector3 Scale) {
            Target.x *= Scale.x;
            Target.y *= Scale.y;
            Target.z *= Scale.z;
            return Target;
        }

        /// <summary>Low-pass filter for a single channel in a multichannel array.</summary>
        /// <param name="Target">Sample array reference</param>
        /// <param name="Last">Last sample</param>
        /// <param name="Samples">Sample count per channel</param>
        /// <param name="Channel">Channel</param>
        /// <param name="Channels">Channels</param>
        public static unsafe void Lowpass(ref float[] Target, ref float Last, int Samples, ref int Channel, ref int Channels) {
            fixed (float* Pointer = Target) {
                for (int Sample = Channel, End = Samples * Channels; Sample < End; Sample += Channels) {
                    float* TargetSample = Pointer + Sample;
                    Last = *TargetSample = .9995f * Last + .0005f * *TargetSample;
                    *TargetSample *= 6;
                    Sample += Channels;
                }
            }
        }

        /// <summary>Low-pass with configurable effect strength and no volume correction.</summary>
        /// <param name="Target">Sample array reference</param>
        /// <param name="Last">Last sample</param>
        /// <param name="Samples">Array length</param>
        /// <param name="Strength">Effect strength</param>
        public static unsafe void Lowpass(ref float[] Target, ref float Last, int Samples, float Strength) {
            float Retain = 1f - Strength;
            fixed (float* Pointer = Target) {
                for (int Sample = 0; Sample < Samples; ++Sample) {
                    float* TargetSample = Pointer + Sample;
                    Last = *TargetSample = Strength * Last + Retain * *TargetSample++;
                }
            }
        }

        /// <summary>Multiplies all values in an array.</summary>
        /// <param name="Target">Array reference</param>
        /// <param name="Count">Array length</param>
        /// <param name="Value">Multiplier</param>
        internal static unsafe void Gain(ref float[] Target, int Count, float Value) {
            for (int Entry = 0; Entry < Count; ++Entry)
                Target[Entry] *= Value;
        }

        /// <summary>Set gain for a channel in a multichannel array.</summary>
        /// <param name="Target">Sample reference</param>
        /// <param name="Samples">Sample count per channel</param>
        /// <param name="Gain">Gain</param>
        /// <param name="Channel">Target channel</param>
        /// <param name="Channels">Channel count</param>
        internal static unsafe void Gain(ref float[] Target, int Samples, float Gain, ref int Channel, ref int Channels) {
            for (int Sample = Channel, End = Samples * Channels; Sample < End; Sample += Channels)
                Target[Sample] *= Gain;
        }

        /// <summary>Fast absolute value of a float.</summary>
        /// <param name="x">Input</param>
        /// <returns>Math.Abs(x), just faster</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe float Abs(float x) {
            int Absolute = (*(int*)&x) & 0x7fffffff;
            return (*(float*)&Absolute);
        }

        /// <summary>
        /// Converts a signal strength (ref = 1) to dB.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float SignalToDb(float Amplitude) {
            return 20 * Mathf.Log10(Amplitude);
        }

        /// <summary>
        /// Converts a dB value (ref = 0) to signal strength.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float DbToSignal(float Amplitude) {
            return Mathf.Pow(10, Amplitude * 0.05f);
        }
    }
}