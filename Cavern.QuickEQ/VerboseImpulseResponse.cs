using System;
using System.Collections.Generic;

using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Contains an impulse response and data that can be calculated from it.</summary>
    public sealed class VerboseImpulseResponse {
        /// <summary>Raw impulse response on the complex plane.</summary>
        public Complex[] ComplexResponse {
            get {
                if (complexResponse != null)
                    return complexResponse;
                return complexResponse = Complex.Parse(response);
            }
        }
        Complex[] complexResponse = null;

        /// <summary>Raw impulse response on the real plane.</summary>
        public float[] Response {
            get {
                if (response != null)
                    return response;
                return response = Measurements.GetRealPart(ComplexResponse);
            }
        }
        float[] response = null;

        /// <summary>Impulse polarity, true if positive.</summary>
        public bool Polarity => Response[Delay] >= 0;

        /// <summary>Get the phase of this impulse relative to a Dirac-delta in radians.</summary>
        public double Phase {
            get {
                if (!double.IsNaN(phase))
                    return phase;
                float reference = Response[Delay], other;
                int otherPos = delay;
                if (reference < 0) {
                    other = float.NegativeInfinity;
                    for (int i = 0; i < response.Length; ++i) {
                        if (other < response[i]) {
                            other = response[i];
                            otherPos = i;
                        }
                    }
                } else {
                    other = float.PositiveInfinity;
                    for (int i = 0; i < response.Length; ++i) {
                        if (other > response[i]) {
                            other = response[i];
                            otherPos = i;
                        }
                    }
                }
                if (other == 0)
                    return phase = 0;
                phase = otherPos < delay ? -1 : 1;
                if (reference >= 0)
                    return phase *= Math.Asin(-other / reference);
                else
                    return phase *= Math.PI * 0.5 - Math.Asin(other / reference);
            }
        }
        double phase = double.NaN;

        /// <summary>How likely this signal is an impulse.</summary>
        public double Impulseness {
            get {
                if (!double.IsNaN(impulseness))
                    return impulseness;
                float peak = Math.Abs(Response[Delay]) * .1f;
                int below = 0;
                for (int i = 0; i < response.Length; ++i)
                    if (Math.Abs(response[i]) < peak)
                        ++below;
                return impulseness = below / (double)response.Length;
            }
        }
        double impulseness = double.NaN;

        /// <summary>Response delay in samples relative to the reference it was calculated from.</summary>
        public int Delay {
            get {
                if (delay != -1)
                    return delay;
                float absPeak = float.NegativeInfinity, absHere;
                for (int pos = 0; pos < Response.Length; ++pos) {
                    absHere = Math.Abs(response[pos]);
                    if (absPeak < absHere) {
                        absPeak = absHere;
                        delay = pos;
                    }
                }
                return delay;
            }
        }
        int delay = -1;

        /// <summary>Get the time in samples where the impulse decays by 60 dB.</summary>
        public int RT60 {
            get {
                if (rt60 != -1)
                    return rt60;
                float target = Math.Abs(Response[Delay] * .001f);
                float[] abs = new float[rt60 = response.Length];
                for (int i = 0; i < rt60; ++i)
                    abs[i] = Math.Abs(response[i]);
                float[] smoothed = GraphUtils.SmoothGraph(abs, 20, 20000, .1f);
                while (smoothed[--rt60] < target && rt60 > delay) ;
                return rt60 -= delay;
            }
        }
        int rt60 = -1;

        /// <summary>Peaks in the impulse response.</summary>
        /// <remarks>Calculated when <see cref="GetPeak(int)"/> is called.</remarks>
        Peak[] peaks = null;

        /// <summary>Create a verbose impulse response from a precalculated impulse response.</summary>
        public VerboseImpulseResponse(Complex[] impulseResponse) => complexResponse = impulseResponse;

        /// <summary>Create a verbose impulse response from a precalculated impulse response.</summary>
        public VerboseImpulseResponse(float[] impulseResponse) => response = impulseResponse;

        /// <summary>Create a verbose impulse response from a reference signal and a recorded response.</summary>
        public VerboseImpulseResponse(float[] reference, float[] response) :
            this(Measurements.IFFT(Measurements.GetFrequencyResponse(reference, response))) { }

        /// <summary>Representation of a peak in the impulse response.</summary>
        public struct Peak {
            /// <summary>Peak time offset in samples.</summary>
            public int Position;
            /// <summary>Gain at that position.</summary>
            public float Size;

            /// <summary>Representation of a peak in the impulse response.</summary>
            /// <param name="position">Peak time offset in samples.</param>
            /// <param name="size">Gain at that position.</param>
            public Peak(int position, float size) {
                Position = position;
                Size = size;
            }

            /// <summary>Returns if a peak is <see cref="Null"/>.</summary>
            public bool IsNull => Position == -1;

            /// <summary>Represents a nonexisting peak.</summary>
            public static Peak Null = new Peak(-1, 0);
        }

        /// <summary>Get the <paramref name="position"/>th peak in the impulse response.</summary>
        public Peak GetPeak(int position) {
            if (peaks == null) {
                List<Peak> peakList = new List<Peak>();
                float[] response = Response;
                float last = Math.Abs(response[0]), abs = Math.Abs(response[1]);
                for (int pos = 2; pos < response.Length; ++pos) {
                    float next = Math.Abs(response[pos]);
                    if (abs > last && abs > next)
                        peakList.Add(new Peak(pos - 1, abs));
                    last = abs;
                    abs = next;
                }
                peakList.Sort((a, b) => b.Size.CompareTo(a.Size));
                peaks = peakList.ToArray();
            }
            return position < peaks.Length ? peaks[position] : Peak.Null;
        }
    }
}