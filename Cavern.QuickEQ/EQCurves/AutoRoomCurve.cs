using System;

using Cavern.QuickEQ.Equalization;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Determines the settings for a <see cref="RoomCurveLikeCurve"/> to follow the bass rise of the user's uncorrected room.
    /// </summary>
    public sealed class AutoRoomCurve : RoomCurveLikeCurve {
        /// <summary>
        /// Determines the settings for a <see cref="RoomCurveLikeCurve"/> to follow the bass rise of the user's uncorrected room.
        /// </summary>
        /// <param name="sourceFrequencyResponses">Measurements of the uncorrected room</param>
        public AutoRoomCurve(Equalizer[] sourceFrequencyResponses) : base(defaultKnee, GetBassRise(sourceFrequencyResponses)) { }

        /// <summary>
        /// Get the uncorrected room's peak LFE gain and set that as the target gain at 20 Hz.
        /// </summary>
        /// <remarks>Normalization of the <paramref name="sourceFrequencyResponses"/> shall be performed by the caller.</remarks>
        static float GetBassRise(Equalizer[] sourceFrequencyResponses) {
            Equalizer average = EQGenerator.AverageRMS(sourceFrequencyResponses);
            average.Smooth(1);
            average.Limit(20, 200);
            return Math.Clamp((int)(average.PeakGain + .5), 3, 10);
        }

        /// <inheritdoc/>
        public override void FromBase64(string source) {
            string data = FromBase64String(source);
            string[] parts = data.Split('|');
            if (parts.Length != 3 || parts[0] != nameof(AutoRoomCurve)) {
                throw new FormatException("Invalid AutoRoomCurve data.");
            }
            trebleSuppression = float.Parse(parts[1]);
            rise = float.Parse(parts[2]);
        }

        /// <inheritdoc/>
        public override string ToBase64() => ToBase64String($"{nameof(AutoRoomCurve)}|{trebleSuppression}|{rise}");
    }
}
