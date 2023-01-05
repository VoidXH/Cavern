using System.Numerics;

namespace Cavern.Remapping {
    partial struct ChannelPrototype {
        /// <summary>
        /// Possible channels to use in layouts.
        /// </summary>
        public static readonly ChannelPrototype
            FrontLeft = new ChannelPrototype(-30, "Front left"),
            FrontRight = new ChannelPrototype(30, "Front right"),
            FrontCenter = new ChannelPrototype(0, "Front center", false, false),
            ScreenLFE = new ChannelPrototype(0, "LFE", true),
            RearLeft = new ChannelPrototype(-150, "Rear left", false, false),
            RearRight = new ChannelPrototype(150, "Rear right", false, false),
            SideLeft = new ChannelPrototype(-110, "Side left", false, false),
            SideRight = new ChannelPrototype(110, "Side right", false, false),
            FrontLeftCenter = new ChannelPrototype(-15, "Front left center"),
            FrontRightCenter = new ChannelPrototype(15, "Front right center"),
            HearingImpaired = new ChannelPrototype(0, "Hearing impaired", false, true),
            VisuallyImpaired = new ChannelPrototype(0, "Visually impaired narrative", false, true),
            Unused = new ChannelPrototype(0, "Unused", false, true),
            MotionData = new ChannelPrototype(0, "Motion data sync", false, true),
            ExternalData = new ChannelPrototype(0, "External sync signal", false, true),
            TopFrontLeft = new ChannelPrototype(-70, -45, "Top front left"),
            TopFrontRight = new ChannelPrototype(70, -45, "Top front right"),
            TopSideLeft = new ChannelPrototype(-110, -45, "Top side left"),
            TopSideRight = new ChannelPrototype(110, -45, "Top side right"),
            SignLanguage = new ChannelPrototype(0, "Sign language video", false, true),
            BottomSurround = new ChannelPrototype(0, 90, "Bottom surround"),
            TopFrontCenter = new ChannelPrototype(0, -45, "Top front center"),
            GodsVoice = new ChannelPrototype(0, -90, "God's voice"),
            RearCenter = new ChannelPrototype(180, "Rear center"),
            WideLeft = new ChannelPrototype(-70, "Wide left"),
            WideRight = new ChannelPrototype(70, "Wide right"),
            TopRearLeft = new ChannelPrototype(-150, -45, "Top rear left"),
            TopRearRight = new ChannelPrototype(150, -45, "Top rear right"),
            TopRearCenter = new ChannelPrototype(180, -45, "Top rear center");


        /// <summary>
        /// Rendering positions of standard channels in a non-standard cube's corners, indexed by <see cref="ReferenceChannel"/>s.
        /// </summary>
        /// <remarks>Internal Cavern channel positions are not the same.</remarks>
        public static readonly Vector3[] AlternativePositions = {
            new Vector3(-1, 0, 1), // FrontLeft
            new Vector3(1, 0, 1), // FrontRight
            new Vector3(0, 0, 1), // FrontCenter
            new Vector3(-1, -1, 1), // ScreenLFE
            new Vector3(-1, 0, -1), // RearLeft
            new Vector3(1, 0, -1), // RearRight
            new Vector3(-1, 0, 0), // SideLeft
            new Vector3(1, 0, 0), // SideRight
            new Vector3(-.5f, 0, 1), // FrontLeftCenter
            new Vector3(.5f, 0, 1), // FrontRightCenter
            new Vector3(0, 0, 1), // HearingImpaired
            new Vector3(0, 0, 1), // VisuallyImpaired
            new Vector3(0, 0, 1), // Unknown
            new Vector3(0, 0, 1), // MotionData
            new Vector3(0, 0, 1), // ExternalData
            new Vector3(-1, 1, 1), // TopFrontLeft
            new Vector3(1, 1, 1), // TopFrontRight
            new Vector3(-1, 1, 0), // TopSideLeft
            new Vector3(1, 1, 0), // TopSideRight
            new Vector3(0, 0, 1), // SignLanguage
            new Vector3(0, -1, 0), // BottomSurround
            new Vector3(0, 1, 1), // TopFrontCenter
            new Vector3(0, 1, 0), // GodsVoice
            new Vector3(0, 0, -1), // RearCenter
            new Vector3(-1, 0, .677419f), // WideLeft
            new Vector3(1, 0, .677419f), // WideRight
            new Vector3(-1, 1, -1), // TopRearLeft
            new Vector3(1, 1, -1), // TopRearRight
            new Vector3(-1, 1, 0) // TopRearCenter
        };

        /// <summary>
        /// Converts the <see cref="ReferenceChannel"/> values to a <see cref="ChannelPrototype"/>.
        /// </summary>
        public static readonly ChannelPrototype[] Mapping = {
            FrontLeft, FrontRight, FrontCenter, ScreenLFE, RearLeft, RearRight, SideLeft, SideRight,
            FrontLeftCenter, FrontRightCenter, HearingImpaired, VisuallyImpaired, Unused, MotionData, ExternalData,
            TopFrontLeft, TopFrontRight, TopSideLeft, TopSideRight, SignLanguage, BottomSurround, TopFrontCenter,
            GodsVoice, RearCenter, WideLeft, WideRight, TopRearLeft, TopRearRight, TopRearCenter
        };

        /// <summary>
        /// Industry standard channel orders for each input channel count.
        /// </summary>
        /// <remarks>Matrices with 8+ channels are DCP orders, with messy standardization, and are
        /// unused in commercial applications. Manual revision before each workflow is recommended
        /// when working with non-5.1 DCPs or content with 8+ channels.</remarks>
        static readonly ReferenceChannel[][] HomeStandardMatrix = {
            new ReferenceChannel[0],
            // 1CH: 1.0 (C)
            new ReferenceChannel[] { ReferenceChannel.FrontCenter},
            // 2CH: 2.0 (L, R)
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            // 3CH: 3.0 (L, R, C) - non-standard
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter },
            // 4CH: 4.0 (L, R, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 5CH: 5.0 (L, R, C, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 6CH: 5.1 (L, R, C, LFE, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 7CH: 7.0 (L, R, C, RL, RR, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 8CH: 7.1 (L, R, C, LFE, RL, RR, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 9CH: 7.0.2 (L, R, C, RL, RR, SL, SR, TFL, TFR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight
            },
            // 10CH: 7.1.2 (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight
            },
            // 11CH: 7.0.4 (L, R, C, RL, RR, SL, SR, TFL, TFR, TRL, TRR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            },
            // 12CH: 7.1.4 (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR, TRL, TRR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            },
            // 13CH: 7.0.6 (L, R, C, RL, RR, SL, SR, TFL, TFC, TFR, TRL, TRC, TRR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight
            },
            // 14CH: 7.1.6 (L, R, C, LFE, RL, RR, SL, SR, TFL, TFC, TFR, TRL, TRC, TRR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight
            },
            // 15CH: 9.0.6 (L, R, C, RL, RR, SL, SR, WL, WR, TFL, TFC, TFR, TRL, TRC, TRR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight
            },
            // 16CH: 9.1.6 (L, R, C, LFE, RL, RR, SL, SR, WL, WR, TFL, TFC, TFR, TRL, TRC, TRR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight
            }
        };

        /// <summary>
        /// Industry standard channel orders for each input channel count.
        /// </summary>
        /// <remarks>Matrices with 8+ channels are DCP orders, with messy standardization, and are
        /// unused in commercial applications. Manual revision before each workflow is recommended
        /// when working with non-5.1 DCPs or content with 8+ channels.</remarks>
        static readonly ReferenceChannel[][] IndustryStandardMatrix = {
            new ReferenceChannel[0],
            // 1CH: 1.0 (C)
            new ReferenceChannel[] { ReferenceChannel.FrontCenter},
            // 2CH: 2.0 (L, R)
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            // 3CH: 3.0 (L, R, C) - non-standard
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter },
            // 4CH: 4.0 (L, R, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 5CH: 5.0 (L, R, C, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 6CH: 5.1 (L, R, C, LFE, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 7CH: 7.0 (L, R, C, RL, RR, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 8CH: 7.1 (L, R, C, LFE, RL, RR, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 9CH: 8.1 (not used) (L, R, C, LFE, RL, RR, SL, SR, RC)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,  ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.RearCenter
            },
            // 10CH: 7.1.2 (out-of-order Cavern DCP) (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight
            },
            // 11CH: 7.1.2.1 (out-of-order Cavern XL DCP) (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR, BS)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.BottomSurround
            },
            // 12CH: 11.1 (L, R, C, LFE, SL, SR, TFL, TFR, TFC, GV, TSL, TSR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopFrontCenter, ReferenceChannel.GodsVoice, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            },
            // 13CH: 12-Track (L, R, C, LFE, RL, RR, TFC, SL, SR, TFL, TFR, TSL, TSR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.TopFrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            },
            // 14CH: 11.1 + MD/ES (L, R, C, LFE, SL, SR, TFL, TFR, TFC, GV, TSL, TSR, MD, ES)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopFrontCenter, ReferenceChannel.GodsVoice,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight, ReferenceChannel.MotionData, ReferenceChannel.ExternalData
            },
            // 15CH: Cavern DCP (L, R, C, LFE, SL, SR, HI, VI, TFL, TFR, RL, RR, MD, ES, SL)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.HearingImpaired, ReferenceChannel.VisuallyImpaired,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.MotionData, ReferenceChannel.ExternalData, ReferenceChannel.SignLanguage
            },
            // 16CH: Cavern XL DCP (L, R, C, LFE, SL, SR, HI, VI, TL, TR, RL, RR, MD, ES, SL, BS)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.HearingImpaired,
                ReferenceChannel.VisuallyImpaired, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.MotionData, ReferenceChannel.ExternalData,
                ReferenceChannel.SignLanguage, ReferenceChannel.BottomSurround
            }
        };

        /// <summary>
        /// Semi-standard (Equalizer APO) channel names.
        /// </summary>
        const string frontLeftMark = "L",
            frontRightMark = "R",
            frontCenterMark = "C",
            screenLFEMark = "LFE",
            subwooferMark = "SUB",
            rearLeftMark = "RL",
            rearRightMark = "RR",
            sideLeftMark = "SL",
            sideRightMark = "SR";
    }
}