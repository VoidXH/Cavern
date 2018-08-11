namespace Cavern.QuickEQ {
    /// <summary>Contains an impulse response and data that can be calculated from it.</summary>
    public class VerboseImpulseResponse {
        /// <summary>Raw impulse response samples.</summary>
        public float[] Response;
        /// <summary>Impulse polarity, true if positive.</summary>
        public bool Polarity;
        /// <summary>Response delay relative to the reference it was calculated from.</summary>
        public int Delay;

        /// <summary>Create a verbose impulse response from a precalculated impulse response.</summary>
        public VerboseImpulseResponse(Complex[] ImpulseResponse) {
            Response = Measurements.GetRealPart(ImpulseResponse);
            float AbsPeak = float.NegativeInfinity;
            for (int Pos = 0, Length = Response.Length; Pos < Length; ++Pos) {
                float AbsHere = CavernUtilities.Abs(Response[Pos]);
                if (AbsPeak < AbsHere) {
                    AbsPeak = AbsHere;
                    Delay = Pos;
                }
            }
            Polarity = AbsPeak >= 0;
        }

        /// <summary>Create a verbose impulse response from a reference signal and a recorded response.</summary>
        public VerboseImpulseResponse(float[] Reference, float[] Response) :
            this(Measurements.GetFrequencyResponse(Reference, Response)) { }
    }
}