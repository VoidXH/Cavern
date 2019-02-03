using System;

namespace Cavern.QuickEQ {
    /// <summary>Contains an impulse response and data that can be calculated from it.</summary>
    public class VerboseImpulseResponse {
        /// <summary>Raw impulse response on the complex plane.</summary>
        public Complex[] ComplexResponse { get; private set; }
        /// <summary>Raw impulse response samples.</summary>
        public float[] Response { get; private set; }
        /// <summary>Impulse polarity, true if positive.</summary>
        public bool Polarity { get; private set; }
        /// <summary>Response delay in samples relative to the reference it was calculated from.</summary>
        public int Delay { get; private set; }

        /// <summary>Peaks in the impulse response.</summary>
        /// <remarks>Calculated when <see cref="GetPeak(int)"/> is called.</remarks>
        Peak[] Peaks = null;

        /// <summary>Create a verbose impulse response from a precalculated impulse response.</summary>
        public VerboseImpulseResponse(Complex[] ImpulseResponse) {
            Response = Measurements.GetRealPart(ComplexResponse = ImpulseResponse);
            float AbsPeak = float.NegativeInfinity;
            for (int Pos = 0, Length = Response.Length; Pos < Length; ++Pos) {
                float AbsHere = Math.Abs(Response[Pos]);
                if (AbsPeak < AbsHere) {
                    AbsPeak = AbsHere;
                    Delay = Pos;
                }
            }
            Polarity = Response[Delay] >= 0;
        }

        /// <summary>Create a verbose impulse response from a reference signal and a recorded response.</summary>
        public VerboseImpulseResponse(float[] Reference, float[] Response) : this(Measurements.IFFT(Measurements.GetFrequencyResponse(Reference, Response))) { }

        /// <summary>Representation of a peak in the impulse response.</summary>
        public struct Peak {
            /// <summary>Peak time offset in samples.</summary>
            public int Position;
            /// <summary>Gain at that position.</summary>
            public float Size;

            /// <summary>Representation of a peak in the impulse response.</summary>
            /// <param name="Position">Peak time offset in samples.</param>
            /// <param name="Size">Gain at that position.</param>
            public Peak(int Position, float Size) {
                this.Position = Position;
                this.Size = Size;
            }
        }

        /// <summary>Get the <paramref name="Position"/>th peak in the impulse response.</summary>
        public Peak GetPeak(int Position) {
            if (Peaks == null) {
                Peaks = new Peak[Response.Length];
                for (int Pos = 0, Length = Peaks.Length; Pos < Length; ++Pos)
                    Peaks[Pos] = new Peak(Pos, Response[Pos]);
                Array.Sort(Peaks, (a, b) => b.Size.CompareTo(a.Size));
            }
            return Peaks[Position];
        }
    }
}