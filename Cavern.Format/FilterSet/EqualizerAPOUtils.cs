using Cavern.Remapping;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Helper functions for handling Equalizer APO configuration files.
    /// </summary>
    public static class EqualizerAPOUtils {
        internal const string frontLeft = "L";
        internal const string frontRight = "R";
        internal const string frontCenter = "C";
        internal const string screenLFE = "SUB";
        internal const string rearLeft = "RL";
        internal const string rearRight = "RR";
        internal const string sideLeft = "SL";
        internal const string sideRight = "SR";

        /// <summary>
        /// Converts Equalizer APO's channel names to <see cref="ReferenceChannel"/> values.
        /// </summary>
        public static ReferenceChannel GetReferenceChannel(string apoName) => apoName switch {
            frontLeft => ReferenceChannel.FrontLeft,
            frontRight => ReferenceChannel.FrontRight,
            frontCenter => ReferenceChannel.FrontCenter,
            screenLFE => ReferenceChannel.ScreenLFE,
            rearLeft => ReferenceChannel.RearLeft,
            rearRight => ReferenceChannel.RearRight,
            sideLeft => ReferenceChannel.SideLeft,
            sideRight => ReferenceChannel.SideRight,
            _ => ReferenceChannel.Unknown,
        };
    }
}