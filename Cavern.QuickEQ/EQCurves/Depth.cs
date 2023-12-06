namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// EQ curve with a sub-bass slope for depth emphasis. Linearly rises by 12 dB down from 60 Hz to 20 Hz,
    /// and optionally linearly decreases from 1 kHz to 20 kHz by 3 dB.
    /// </summary>
    public class Depth : RoomCurveLikeCurve {
        /// <summary>
        /// EQ curve with a sub-bass slope for depth emphasis. Linearly rises by 12 dB down from 60 Hz to 20 Hz,
        /// and optionally linearly decreases from 1 kHz to 20 kHz by 3 dB.
        /// </summary>
        public Depth() : base(60, 12) { }
    }
}