namespace Cavern.Channels
{
    /// <summary>
    /// Helper functions for handling Equalizer APO configuration files.
    /// </summary>
    public static class EqualizerAPOUtils {
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

        /// <summary>
        /// Get Equalizer APO's label for a channel of a given channel count.
        /// </summary>
        public static string GetChannelLabel(int channel, int channels) {
            if (channel > 7) {
                return "CH" + (channel + 1);
            }
            if (channels < 7) {
                if (channels < 5) {
                    return APO40[channel];
                }
                return APO51[channel];
            }
            return APO71[channel];
        }

        const string frontLeft = "L";
        const string frontRight = "R";
        const string frontCenter = "C";
        const string screenLFE = "SUB";
        const string rearLeft = "RL";
        const string rearRight = "RR";
        const string sideLeft = "SL";
        const string sideRight = "SR";

        /// <summary>
        /// Channels in the 4.0 layout of Equalizer APO.
        /// </summary>
        static readonly string[] APO40 = { frontLeft, frontRight, rearLeft, rearRight };

        /// <summary>
        /// Channels in the 5.1 layout of Equalizer APO.
        /// </summary>
        static readonly string[] APO51 = { frontLeft, frontRight, frontCenter, screenLFE, sideLeft, sideRight };

        /// <summary>
        /// Channels in the 7.1 layout of Equalizer APO.
        /// </summary>
        static readonly string[] APO71 = { frontLeft, frontRight, frontCenter, screenLFE, rearLeft, rearRight, sideLeft, sideRight };
    }
}