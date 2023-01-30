using Cavern.Channels;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    static class ADMConsts {
        /// <summary>
        /// Standard ADM channel names indexed by <see cref="ReferenceChannel"/>s.
        /// </summary>
        public static readonly string[] channelNames = {
            "RoomCentricLeft",
            "RoomCentricRight",
            "RoomCentricCenter",
            "RoomCentricLFE",
            "RoomCentricLeftRearSurround",
            "RoomCentricRightRearSurround",
            "RoomCentricLeftSideSurround",
            "RoomCentricRightSideSurround",
            "RoomCentricLeftCenter",
            "RoomCentricRightCenter",
            "HearingImpaired",
            "VisuallyImpairedNarrative",
            "Unknown",
            "MotionData",
            "ExternalSync",
            "RoomCentricLeftTop",
            "RoomCentricRightTop",
            "RoomCentricLeftTopSurround",
            "RoomCentricRightTopSurround",
            "SignLanguageVideo",
            "RoomCentricCenterBottomSurround",
            "RoomCentricCenterTop",
            "RoomCentricCenterTopSurround",
            "RoomCentricCenterRearSurround",
            "RoomCentricLeftWideSurround",
            "RoomCentricRightWideSurround",
            "RoomCentricLeftTopRear",
            "RoomCentricRightTopRear",
            "RoomCentricCenterTopRear"
        };

        /// <summary>
        /// Standard ADM channel labels indexed by <see cref="ReferenceChannel"/>s.
        /// </summary>
        public static readonly string[] channelLabels = {
            "RC_L",
            "RC_R",
            "RC_C",
            "RC_LFE",
            "RC_Lrs",
            "RC_Rrs",
            "RC_Lss",
            "RC_Rss",
            "RC_Lc",
            "RC_Rc",
            "HI",
            "VI",
            "",
            "MD",
            "EX",
            "RC_Lt",
            "RC_Rt",
            "RC_Lts",
            "RC_Rts",
            "SL",
            "RC_Cbs",
            "RC_Ct",
            "RC_Cts",
            "RC_Crs",
            "RC_Lws",
            "RC_Rws",
            "RC_Ltr",
            "RC_Rtr",
            "RC_Ctr"
        };
    }
}