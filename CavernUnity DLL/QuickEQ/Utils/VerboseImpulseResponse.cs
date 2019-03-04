using System;
using System.Collections.Generic;

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

            /// <summary>Returns if a peak is <see cref="Null"/>.</summary>
            public bool IsNull => Position == -1;

            /// <summary>Represents a nonexisting peak.</summary>
            public static Peak Null = new Peak(-1, 0);
        }

        /// <summary>Get the <paramref name="Position"/>th peak in the impulse response.</summary>
        public Peak GetPeak(int Position) {
            if (Peaks == null) {
                List<Peak> PeakList = new List<Peak>();
                float Last = Math.Abs(Response[0]), Abs = Math.Abs(Response[1]);
                for (int Pos = 2, Length = Response.Length; Pos < Length; ++Pos) {
                    float Next = Math.Abs(Response[Pos]);
                    if (Abs > Last && Abs > Next)
                        PeakList.Add(new Peak(Pos - 1, Abs));
                    Last = Abs;
                    Abs = Next;
                }
                PeakList.Sort((a, b) => b.Size.CompareTo(a.Size));
                Peaks = PeakList.ToArray();
            }
            return Position < Peaks.Length ? Peaks[Position] : Peak.Null;
        }
    }
}