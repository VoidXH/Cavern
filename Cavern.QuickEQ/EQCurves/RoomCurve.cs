using System;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Frequently used target curve for very small rooms. Linearly rises by 3 dB down from 200 Hz to 20 Hz,
    /// and optionally linearly decreases from 1 kHz to 20 kHz by 3 dB.
    /// </summary>
    public class RoomCurve : RoomCurveLikeCurve {
        /// <summary>
        /// Frequently used target curve for very small rooms. Linearly rises by 3 dB down from 200 Hz to 20 Hz,
        /// and optionally linearly decreases from 1 kHz to 20 kHz by 3 dB.
        /// </summary>
        public RoomCurve() : base(defaultKnee, 3) { }

        /// <inheritdoc/>
        public override void FromBase64(string source) => base.FromBase64(source);

        /// <inheritdoc/>
        public override string ToBase64() => base.ToBase64();
    }
}