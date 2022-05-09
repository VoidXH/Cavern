using System;

namespace Cavern.Remapping {
    /// <summary>
    /// Light audio channel information structure.
    /// </summary>
    public struct ChannelPrototype : IEquatable<ChannelPrototype> {
        /// <summary>
        /// Horizontal axis angle.
        /// </summary>
        public readonly float Y;

        /// <summary>
        /// Vertical axis angle.
        /// </summary>
        public readonly float X;

        /// <summary>
        /// Channel name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// True if the channel is used for Low Frequency Effects.
        /// </summary>
        public readonly bool LFE;

        /// <summary>
        /// Mute status.
        /// </summary>
        /// <remarks>Some channels should not be played back on the spatial master, like hearing/visually impaired tracks.</remarks>
        public readonly bool Muted;

        /// <summary>
        /// Standard channel constructor.
        /// </summary>
        /// <param name="y">Horizontal axis angle</param>
        /// <param name="name">Channel name</param>
        /// <param name="LFE">True if the channel is used for Low Frequency Effects</param>
        /// <param name="muted">Mute status</param>
        ChannelPrototype(float y, string name, bool LFE = false, bool muted = false) {
            X = 0;
            Y = y;
            Name = name;
            this.LFE = LFE;
            Muted = muted;
        }

        /// <summary>
        /// Spatial channel constructor.
        /// </summary>
        /// <param name="y">Horizontal axis angle</param>
        /// <param name="x">Vertical axis angle</param>
        /// <param name="name">Channel name</param>
        ChannelPrototype(float y, float x, string name) {
            Y = y;
            X = x;
            Name = name;
            LFE = Muted = false;
        }

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

        const string frontLeftMark = "L",
            frontRightMark = "R",
            frontCenterMark = "C",
            screenLFEMark = "LFE",
            subwooferMark = "SUB",
            rearLeftMark = "RL",
            rearRightMark = "RR",
            sideLeftMark = "SL",
            sideRightMark = "SR";

        /// <summary>
        /// Converts a standard channel shorthand to a <see cref="ChannelPrototype"/>.
        /// </summary>
        public static ChannelPrototype FromStandardName(string name) {
            switch (name) {
                case frontLeftMark:
                    return FrontLeft;
                case frontRightMark:
                    return FrontRight;
                case frontCenterMark:
                    return FrontCenter;
                case screenLFEMark:
                case subwooferMark:
                    return ScreenLFE;
                case rearLeftMark:
                    return RearLeft;
                case rearRightMark:
                    return RearRight;
                case sideLeftMark:
                    return SideLeft;
                case sideRightMark:
                    return SideRight;
                default:
                    return Unused;
            }
        }

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
        /// <remarks>Matrices with 8+ channels are DCP orders, with messy standardization, and are unused in commercial applications.
        /// Manual revision before each workflow is recommended when working with non-5.1 DCPs or content with 8+ channels.</remarks>
        static readonly ReferenceChannel[][] StandardMatrix = {
            new ReferenceChannel[0],
            // 1CH: 1.0 (C)
            new ReferenceChannel[] { ReferenceChannel.FrontCenter},
            // 2CH: 2.0 (L, R)
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            // 3CH: 3.0 (L, R, C) - non-standard
            new ReferenceChannel[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter },
            // 4CH: 4.0 (L, R, SL, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight
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
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight
            },
            // 9CH: 8.1 (not used) (L, R, C, LFE, RL, RR, SL, SR, RC)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
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
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            },
            // 14CH: 13.1 (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR, TFC, GV, TSL, TSR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopFrontCenter, ReferenceChannel.GodsVoice,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            },
            // 15CH: Cavern DCP (L, R, C, LFE, SL, SR, HI, VI, TFL, TFR, RL, RR, MD, ES, SL)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.HearingImpaired, ReferenceChannel.VisuallyImpaired,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.MotionData, ReferenceChannel.ExternalData, ReferenceChannel.SignLanguage
            },
            // 16CH: Cavern XL DCP (L, R, C, LFE, SL, SR, HI, VI, TL, TR, RL, RR, MD, ES, SL, BS)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.HearingImpaired, ReferenceChannel.VisuallyImpaired,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.MotionData, ReferenceChannel.ExternalData, ReferenceChannel.SignLanguage, ReferenceChannel.BottomSurround
            }
        };

        /// <summary>
        /// Gets an industry standard channel matrix for a given channel count.
        /// </summary>
        /// <remarks>If the channel count is larger than the largest supported layout, it will be filled with
        /// <see cref="ReferenceChannel.Unknown"/>.</remarks>
        public static ReferenceChannel[] GetStandardMatrix(int count) {
            int subcount = Math.Min(count, StandardMatrix.Length);
            ReferenceChannel[] matrix = StandardMatrix[count];
            if (subcount != count) {
                Array.Resize(ref matrix, count);
                for (int i = subcount; i < count; ++i)
                    matrix[i] = ReferenceChannel.Unknown;
            }
            return matrix;
        }

        /// <summary>
        /// Get a <paramref name="channel"/>'s <see cref="ChannelPrototype"/> of the standard layout
        /// with a given number of <paramref name="channels"/>.
        /// </summary>
        public static ChannelPrototype Get(int channel, int channels) {
            int prototypeID = (int)StandardMatrix[channels][channel];
            return Mapping[prototypeID];
        }

        /// <summary>
        /// Convert a mapping of <see cref="ReferenceChannel"/>s to <see cref="ChannelPrototype"/>s.
        /// </summary>
        public static ChannelPrototype[] Get(ReferenceChannel[] source) {
            ChannelPrototype[] result = new ChannelPrototype[source.Length];
            for (int i = 0; i < source.Length; ++i)
                result[i] = Mapping[(int)source[i]];
            return result;
        }

        /// <summary>
        /// Check if two channel prototypes are the same.
        /// </summary>
        public bool Equals(ChannelPrototype other) => X == other.X && Y == other.Y && LFE == other.LFE;

        /// <summary>
        /// Human-readable channel prototype data.
        /// </summary>
        public override string ToString() {
            string basic = $"{(LFE ? Name + "(LFE)" : Name)} ({X}; {Y})";
            if (Muted)
                return basic + " (muted)";
            return basic;
        }
    }
}