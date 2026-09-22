using System;

using Cavern.Channels;

namespace Cavern.Format.FilterSet.Consts {
    /// <summary>
    /// Database of MultEQ-X-related information.
    /// </summary>
    internal static class MQXConsts {
        /// <summary>
        /// Child of the root element containing channel naming and positioning.
        /// </summary>
        internal const string channelMappingKey = "_channelDataMap";

        /// <summary>
        /// Channel layout for each channel count in a MultEQ-X configuration file.
        /// </summary>
        internal static readonly ReferenceChannel[][] matrix = new ReferenceChannel[][] {
            Array.Empty<ReferenceChannel>(),
            new ReferenceChannel[] { ReferenceChannel.FrontCenter },
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter },
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.ScreenLFE },
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.ScreenLFE, ReferenceChannel.ScreenLFE },
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.ScreenLFE }
        };

        /// <summary>
        /// Values of MultEQ fields for <see cref="ReferenceChannel"/>s.
        /// </summary>
        internal static readonly (ReferenceChannel channel, string designation, string name, string pairDesignation, string pair, string location)[] labeling = {
            (ReferenceChannel.FrontLeft, "FL", "Front Left", "F_", "Front", "FL"),
            (ReferenceChannel.FrontRight, "FR", "Front Right", "F_", "Front", "FR"),
            (ReferenceChannel.FrontCenter, "C", "Center", "C", "Center", "Center, Front"),
            (ReferenceChannel.ScreenLFE, "SW1", "Subwoofer 1", "SW1", "Subwoofer 1", "Subwoofer, Position1"),
            (ReferenceChannel.SideLeft, "SLA", "Surround Left", "S_A", "Surround", "Left, Surround, Position1"),
            (ReferenceChannel.SideRight, "SRA", "Surround Right", "S_A", "Surround", "Right, Surround, Position1"),
            (ReferenceChannel.RearLeft, "SBL", "Surround Back Left", "SB_", "Surround Back", "Left, Back"),
            (ReferenceChannel.RearRight, "SBR", "Surround Back Right", "SB_", "Surround Back", "Right, Back"),
            (ReferenceChannel.TopFrontLeft, "FHL", "Front Height Left", "FH_", "Front Height", "FL, Height"),
            (ReferenceChannel.TopFrontCenter, "CH", "Center Height", "CH", "Center Height", "Overhead"),
            (ReferenceChannel.TopFrontRight, "FHR", "Front Height Right", "FH_", "Front Height", "FR, Height"),
            (ReferenceChannel.TopSideLeft, "TML", "Top Middle Left", "TM_", "Top Middle", "Left, Surround, Top"),
            (ReferenceChannel.TopSideRight, "TMR", "Top Middle Right", "TM_", "Top Middle", "Right, Surround, Top"),
            (ReferenceChannel.TopRearLeft, "RHL", "Rear Height Left", "RH_", "Rear Height", "Left, Rear, Height"),
            (ReferenceChannel.TopRearRight, "RHR", "Rear Height Right", "RH_", "Rear Height", "Right, Rear, Height"),
            (ReferenceChannel.GodsVoice, "TS", "Top Surround", "TS", "Top Surround", "Surround, Top"),
            (ReferenceChannel.TopFrontLeft, "FDL", "Front Dolby Left", "FD_", "Front Dolby", "FL, Dolby"),
            (ReferenceChannel.TopFrontRight, "FDR", "Front Dolby Right", "FD_", "Front Dolby", "FR, Dolby"),
            (ReferenceChannel.TopRearLeft, "BDL", "Back Dolby Left", "BD_", "Back Dolby", "Left, Back, Dolby"),
            (ReferenceChannel.TopRearRight, "BDR", "Back Dolby Right", "BD_", "Back Dolby", "Right, Back, Dolby"),
        };
    }
}
