using System;
using System.Collections.Generic;

using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>
    /// Contains an impulse response and data that can be calculated from it.
    /// </summary>
    public sealed class VerboseImpulseResponse {
        /// <summary>
        /// Raw impulse response on the complex plane.
        /// </summary>
        /// <remarks>This is not the transfer function, not the FFT of the impulse response, but the response itself.</remarks>
        public Complex[] ComplexResponse {
            get {
                if (complexResponse != null) {
                    return complexResponse;
                }
                return complexResponse = Complex.Parse(response);
            }
        }
        Complex[] complexResponse;

        /// <summary>
        /// Raw impulse response on the real plane.
        /// </summary>
        public float[] Response {
            get {
                if (response != null) {
                    return response;
                }
                return response = Measurements.GetRealPart(ComplexResponse);
            }
        }
        float[] response;

        /// <summary>
        /// Impulse polarity, true if positive.
        /// </summary>
        public bool Polarity {
            get {
                if (Delay >= 0) {
                    return Response[delay] >= 0;
                }
                return true;
            }
        }

        /// <summary>
        /// Get the phase of this impulse relative to a Dirac-delta in radians.
        /// </summary>
        public double Phase {
            get {
                if (!double.IsNaN(phase)) {
                    return phase;
                }
                if (Delay < 0) {
                    return double.NaN;
                }
                float reference = Response[delay], other;
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
                if (other == 0) {
                    return phase = 0;
                }
                phase = otherPos < delay ? -1 : 1;
                if (reference >= 0) {
                    return phase *= Math.Asin(-other / reference);
                } else {
                    return phase *= Math.PI * 0.5 - Math.Asin(other / reference);
                }
            }
        }
        double phase = double.NaN;

        /// <summary>
        /// How likely this signal is an impulse.
        /// </summary>
        public double Impulseness {
            get {
                if (!double.IsNaN(impulseness)) {
                    return impulseness;
                }
                if (Delay < 0) {
                    return double.NaN;
                }
                float peak = Math.Abs(Response[delay]) * .1f;
                int below = 0;
                for (int i = 0; i < response.Length; ++i) {
                    if (Math.Abs(response[i]) < peak) {
                        ++below;
                    }
                }
                return impulseness = below / (double)response.Length;
            }
        }
        double impulseness = double.NaN;

        /// <summary>
        /// Response delay in samples relative to the reference it was calculated from.
        /// </summary>
        public int Delay {
            get {
                if (delay != -1) {
                    return delay;
                }
                return delay = GetDelay(Response);
            }
        }
        int delay = -1;

        /// <summary>
        /// Get the time in samples where the impulse decays by 60 dB.
        /// </summary>
        public int RT60 {
            get {
                if (rt60 != -1) {
                    return rt60;
                }
                if (Delay < 0) {
                    return 0;
                }
                float target = Math.Abs(Response[delay] * .001f);
                float[] abs = new float[rt60 = response.Length];
                for (int i = 0; i < rt60; ++i) {
                    abs[i] = Math.Abs(response[i]);
                }
                float[] smoothed = GraphUtils.SmoothGraph(abs, 20, 20000, .1f);
                do {
                    --rt60;
                } while (smoothed[rt60] < target && rt60 > delay);
                return rt60 -= delay;
            }
        }
        int rt60 = -1;

        /// <summary>
        /// Peaks in the impulse response.
        /// </summary>
        /// <remarks>Calculated when <see cref="GetPeak(int)"/> is called.</remarks>
        Peak[] peaks;

        /// <summary>
        /// Create a verbose impulse response from a precalculated impulse response.
        /// </summary>
        public VerboseImpulseResponse(Complex[] impulseResponse) => complexResponse = impulseResponse;

        /// <summary>
        /// Create a verbose impulse response from a precalculated impulse response.
        /// </summary>
        public VerboseImpulseResponse(float[] impulseResponse) => response = impulseResponse;

        /// <summary>
        /// Create a verbose impulse response from a reference signal and a recorded response.
        /// </summary>
        public VerboseImpulseResponse(float[] reference, float[] response) :
            this(Measurements.GetFrequencyResponse(reference, response).IFFT()) { }

        /// <summary>
        /// Representation of a peak in the impulse response.
        /// </summary>
        public readonly struct Peak : IEquatable<Peak> {
            /// <summary>
            /// Peak time offset in samples.
            /// </summary>
            public int Position { get; }
            /// <summary>
            /// Gain at that position.
            /// </summary>
            public float Size { get; }

            /// <summary>
            /// Representation of a peak in the impulse response.
            /// </summary>
            /// <param name="position">Peak time offset in samples.</param>
            /// <param name="size">Gain at that position.</param>
            public Peak(int position, float size) {
                Position = position;
                Size = size;
            }

            /// <summary>
            /// Returns if a peak is <see cref="Null"/>.
            /// </summary>
            public bool IsNull => Position == -1;

            /// <summary>
            /// Represents a nonexisting peak.
            /// </summary>
            public static readonly Peak Null = new Peak(-1, 0);

            /// <summary>
            /// Check if two peaks are equal.
            /// </summary>
            public bool Equals(Peak other) => Position != other.Position && Size != other.Size;
        }

        /// <summary>
        /// Get the delay of an impulse response in samples. In this case, delay means the index of the highest absolute value sample.
        /// </summary>
        public static int GetDelay(float[] response) {
            int result = 0;
            float absPeak = float.NegativeInfinity, absHere;
            for (int pos = 0; pos < response.Length; ++pos) {
                absHere = Math.Abs(response[pos]);
                if (absPeak < absHere) {
                    absPeak = absHere;
                    result = pos;
                }
            }
            return result;
        }

        /// <summary>
        /// Get the <paramref name="position"/>th peak in the impulse response.
        /// </summary>
        public Peak GetPeak(int position) {
            if (peaks == null) {
                List<Peak> peakList = new List<Peak>();
                float[] raw = Response;
                float last = Math.Abs(raw[0]), abs = Math.Abs(raw[1]);
                for (int pos = 2; pos < raw.Length; ++pos) {
                    float next = Math.Abs(raw[pos]);
                    if (abs > last && abs > next) {
                        peakList.Add(new Peak(pos - 1, abs));
                    }
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