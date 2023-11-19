using System;
using System.Numerics;

namespace Cavern.Channels {
    public partial struct ChannelPrototype : IEquatable<ChannelPrototype> {
        /// <summary>
        /// Possible channels to use in layouts.
        /// </summary>
        public static readonly ChannelPrototype
            FrontLeft = new ChannelPrototype(-30, "Front left"),
            FrontRight = new ChannelPrototype(30, "Front right"),
            FrontCenter = new ChannelPrototype(0, "Front center", false, false),
            ScreenLFE = new ChannelPrototype(0, "Low frequency effects", true),
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
        /// Standard 1.0.0 setup (C).
        /// </summary>
        public static readonly ReferenceChannel[] ref100 = { ReferenceChannel.FrontCenter };

        /// <summary>
        /// Standard 2.0.0 setup (L, R).
        /// </summary>
        public static readonly ReferenceChannel[] ref200 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight };

        /// <summary>
        /// Standard 3.0.0 setup (L, R, C).
        /// </summary>
        public static readonly ReferenceChannel[] ref300 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter };

        /// <summary>
        /// Standard 4.0.0 setup (L, R, SL, SR).
        /// </summary>
        public static readonly ReferenceChannel[] ref400 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight };

        /// <summary>
        /// Standard 5.0.0 setup (L, R, C, SL, SR).
        /// </summary>
        public static readonly ReferenceChannel[] ref500 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.SideLeft, ReferenceChannel.SideRight };

        /// <summary>
        /// Standard 5.1.0 setup (L, R, C, LFE, SL, SR).
        /// </summary>
        public static readonly ReferenceChannel[] ref510 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight };

        /// <summary>
        /// Standard 5.1.2 setup (L, R, C, LFE, SL, SR, TSL, TSR).
        /// </summary>
        public static readonly ReferenceChannel[] ref512 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,  ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight };

        /// <summary>
        /// Standard 5.1.4 setup (L, R, C, LFE, SL, SR, TFL, TFR, TRL, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] ref514 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight };

        /// <summary>
        /// 5.1.6 setup with top sides (L, R, C, LFE, SL, SR, TFL, TFR, TSL, TSR, TSR, TRL, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] ref516 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight };

        /// <summary>
        /// Standard 5.1.6 setup (L, R, C, LFE, SL, SR, TFL, TFC, TFR, TRL, TRC, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] wav516 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight };

        /// <summary>
        /// Standard 7.0.0 setup (L, R, C, RL, RR, SL, SR).
        /// </summary>
        public static readonly ReferenceChannel[] ref700 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight };

        /// <summary>
        /// Standard 7.0.2 setup (L, R, C, RL, RR, SL, SR, TFL, TFR).
        /// </summary>
        public static readonly ReferenceChannel[] ref702 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight };

        /// <summary>
        /// Standard 7.0.4 setup (L, R, C, RL, RR, SL, SR, TFL, TFR, TRL, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] ref704 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight };

        /// <summary>
        /// WAVE-standard 7.0.6 setup (L, R, C, RL, RR, SL, SR, TFL, TFC, TFR, TRL, TRC, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] wav706 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight };

        /// <summary>
        /// Standard 7.1.0 setup (L, R, C, LFE, RL, RR, SL, SR).
        /// </summary>
        public static readonly ReferenceChannel[] ref710 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight };

        /// <summary>
        /// Standard 7.1.2 setup (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR).
        /// </summary>
        public static readonly ReferenceChannel[] ref712 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight };

        /// <summary>
        /// 7.1.2.1 (out-of-order Cavern XL DCP) setup (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR, BS).
        /// </summary>
        public static readonly ReferenceChannel[] ref712_1 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.BottomSurround };

        /// <summary>
        /// Standard 7.1.4 setup (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR, TRL, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] ref714 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight };

        /// <summary>
        /// 7.1.6 setup with top sides (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR, TSL, TSR, TSR, TRL, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] ref716 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight };

        /// <summary>
        /// Standard 7.1.6 setup (L, R, C, LFE, RL, RR, SL, SR, TFL, TFC, TFR, TRL, TRC, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] wav716 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight };

        /// <summary>
        /// DCP standard 8.1.0 setup (L, R, C, LFE, RL, RR, SL, SR, RC).
        /// </summary>
        public static readonly ReferenceChannel[] ref810 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,  ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.RearCenter };

        /// <summary>
        /// Standard 9.0.6 setup (L, R, C, RL, RR, SL, SR, WL, WR, TFL, TFC, TFR, TRL, TRC, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] wav906 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.WideLeft, ReferenceChannel.WideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight };

        /// <summary>
        /// Standard 9.1.4 setup (L, R, C, LFE, RL, RR, SL, SR, WL, WR, TFL, TFR, TRL, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] ref914 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.WideLeft, ReferenceChannel.WideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight };

        /// <summary>
        /// 9.1.6 setup with top sides (L, R, C, LFE, RL, RR, SL, SR, WL, WR, TFL, TFR, TSL, TSR, TSR, TRL, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] ref916 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.WideLeft, ReferenceChannel.WideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight };

        /// <summary>
        /// Standard 9.1.6 setup (L, R, C, LFE, RL, RR, SL, SR, WL, WR, TFL, TFC, TFR, TRL, TRC, TRR).
        /// </summary>
        public static readonly ReferenceChannel[] wav916 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.WideLeft, ReferenceChannel.WideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight };

        /// <summary>
        /// Standard 11.1 DCP (L, R, C, LFE, SL, SR, TFL, TFR, TFC, GV, TSL, TSR).
        /// </summary>
        public static readonly ReferenceChannel[] ref111 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopFrontCenter, ReferenceChannel.GodsVoice, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight };

        /// <summary>
        /// Standard 11.1 DCP with MD/ES signals (L, R, C, LFE, SL, SR, TFL, TFR, TFC, GV, TSL, TSR, MD, ES).
        /// </summary>
        public static readonly ReferenceChannel[] ref111Plus = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopFrontCenter, ReferenceChannel.GodsVoice, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight, ReferenceChannel.MotionData, ReferenceChannel.ExternalData };

        /// <summary>
        /// Standard 12-Track DCP (L, R, C, LFE, RL, RR, TFC, SL, SR, TFL, TFR, TSL, TSR).
        /// </summary>
        public static readonly ReferenceChannel[] ref121 = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.TopFrontCenter, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight };

        /// <summary>
        /// Standard Cavern DCP with all signals (L, R, C, LFE, SL, SR, HI, VI, TFL, TFR, RL, RR, MD, ES, SL).
        /// </summary>
        public static readonly ReferenceChannel[] refCavern = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.HearingImpaired, ReferenceChannel.VisuallyImpaired, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.MotionData, ReferenceChannel.ExternalData, ReferenceChannel.SignLanguage };

        /// <summary>
        /// Standard Cavern XL DCP with all signals (L, R, C, LFE, SL, SR, HI, VI, TFL, TFR, RL, RR, MD, ES, SL, BS).
        /// </summary>
        public static readonly ReferenceChannel[] refCavernXL = { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.HearingImpaired, ReferenceChannel.VisuallyImpaired, ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.MotionData, ReferenceChannel.ExternalData, ReferenceChannel.SignLanguage, ReferenceChannel.BottomSurround };

        /// <summary>
        /// Industry standard channel orders for each input channel count.
        /// </summary>
        /// <remarks>Matrices with 8+ channels are DCP orders, with messy standardization, and are
        /// unused in commercial applications. Manual revision before each workflow is recommended
        /// when working with non-5.1 DCPs or content with 8+ channels.</remarks>
        static readonly ReferenceChannel[][] HomeStandardMatrix = {
            Array.Empty<ReferenceChannel>(), ref100, ref200, ref300, ref400, ref500, ref510, ref700, ref710,
            ref702, ref712, ref704, ref714, wav706, wav716, wav906, wav916
        };

        /// <summary>
        /// Industry standard channel orders for each input channel count.
        /// </summary>
        /// <remarks>Matrices with 8+ channels are DCP orders, with messy standardization, and are
        /// unused in commercial applications. Manual revision before each workflow is recommended
        /// when working with non-5.1 DCPs or content with 8+ channels.</remarks>
        static readonly ReferenceChannel[][] IndustryStandardMatrix = {
            Array.Empty<ReferenceChannel>(), ref100, ref200, ref300, ref400, ref500, ref510, ref700, ref710,
            ref810, ref712, ref712_1, ref111, ref121, ref111Plus, refCavern, refCavernXL
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