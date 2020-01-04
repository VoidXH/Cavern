namespace Cavern.Cavernize {
    /// <summary>Named channel structure.</summary>
    class CavernizeChannel {
        /// <summary>Horizontal axis angle.</summary>
        public readonly float Y;
        /// <summary>Vertical axis angle.</summary>
        public readonly float X;
        /// <summary>Channel name.</summary>
        public readonly string Name;
        /// <summary>True if the channel is used for Low Frequency Effects.</summary>
        public readonly bool LFE;
        /// <summary>Mute status.</summary>
        public readonly bool Muted;

        /// <summary>Standard channel constructor.</summary>
        /// <param name="y">Horizontal axis angle</param>
        /// <param name="name">Channel name</param>
        /// <param name="LFE">True if the channel is used for Low Frequency Effects</param>
        /// <param name="muted">Mute status</param>
        CavernizeChannel(float y, string name, bool LFE = false, bool muted = false) {
            X = 0;
            Y = y;
            Name = name;
            this.LFE = LFE;
            Muted = muted;
        }

        /// <summary>Spatial channel constructor.</summary>
        /// <param name="y">Horizontal axis angle</param>
        /// <param name="x">Vertical axis angle</param>
        /// <param name="name">Channel name</param>
        CavernizeChannel(float y, float x, string name) {
            Y = y;
            X = x;
            Name = name;
            LFE = Muted = false;
        }

        /// <summary>Possible channels to use in layouts.</summary>
        public static CavernizeChannel
            FrontLeft = new CavernizeChannel(-30, "Front left"),
            FrontRight = new CavernizeChannel(30, "Front right"),
            FrontCenter = new CavernizeChannel(0, "Front center", false, false),
            ScreenLFE = new CavernizeChannel(0, "LFE", true),
            SideLeft = new CavernizeChannel(-110, "Side left", false, false),
            SideRight = new CavernizeChannel(110, "Side right", false, false),
            RearLeft = new CavernizeChannel(-150, "Rear left", false, false),
            RearRight = new CavernizeChannel(150, "Rear right", false, false),
            //FrontLeftCenter = new CavernizeChannel(-15, "Front left center"),
            //FrontRightCenter = new CavernizeChannel(15, "Front right center"),
            //HearingImpaired = new CavernizeChannel(0, "Hearing impaired", false, true),
            //VisuallyImpaired = new CavernizeChannel(0, "Visually impaired narrative", false, true),
            //Unused = new CavernizeChannel(0, "Unused", false, true),
            //MotionData = new CavernizeChannel(0, "Motion data sync", false, true),
            //ExternalData = new CavernizeChannel(0, "External sync signal", false, true),
            //TopFrontLeft = new CavernizeChannel(-70, -45, "Top front left"),
            //TopFrontRight = new CavernizeChannel(70, -45, "Top front right"),
            //TopSideLeft = new CavernizeChannel(-130, -45, "Top side left"),
            //TopSideRight = new CavernizeChannel(130, -45, "Top side right"),
            //SignLanguage = new CavernizeChannel(0, "Sign language video", false, true),
            //BottomSurround = new CavernizeChannel(0, 90, "Bottom surround"),
            //TopFrontCenter = new CavernizeChannel(0, -45, "Top front center"),
            //GodsVoice = new CavernizeChannel(0, -90, "God's voice"),
            RearCenter = new CavernizeChannel(180, "Rear center");

        /// <summary>Possible upmix targets, always created.</summary>
        public static readonly CavernizeChannel[] UpmixTargets = { FrontCenter, SideLeft, SideRight, RearLeft, RearRight };

        /// <summary>Default channel orders for each input channel count.</summary>
        public static readonly CavernizeChannel[][] StandardMatrix = {
            new CavernizeChannel[0],
            // 1CH: 1.0 (C)
            new CavernizeChannel[]{FrontCenter},
            // 2CH: 2.0 (L, R)
            new CavernizeChannel[]{FrontLeft, FrontRight},
            // 3CH: 3.0 (L, R, C) - non-standard
            new CavernizeChannel[]{FrontLeft, FrontRight, FrontCenter},
            // 4CH: 4.0 (L, R, SL, SR)
            new CavernizeChannel[]{FrontLeft, FrontRight, SideLeft, SideRight},
            // 5CH: 5.0 (L, R, C, SL, SR)
            new CavernizeChannel[]{FrontLeft, FrontRight, FrontCenter, SideLeft, SideRight},
            // 6CH: 5.1 (L, R, C, LFE, SL, SR)
            new CavernizeChannel[]{FrontLeft, FrontRight, FrontCenter, ScreenLFE, SideLeft, SideRight},
            // 7CH: 6.1 (L, R, C, LFE, SL, SR, RC)
            new CavernizeChannel[]{FrontLeft, FrontRight, FrontCenter, ScreenLFE, SideLeft, SideRight, RearCenter},
            // 8CH: 7.1 (L, R, C, LFE, RL, RR, SL, SR)
            new CavernizeChannel[]{FrontLeft, FrontRight, FrontCenter, ScreenLFE, RearLeft, RearRight, SideLeft, SideRight},
            // These are DCP orders, with messy standardization, and are unused in commercial applications. Revision is recommended for Cavernizing non-5.1 DCPs.
            //new int[]{0, 1, 2, 3, 6, 7, 4, 5, 8}, // 9CH: 8.1 (not used) (L, R, C, LFE, RL, RR, SL, SR, RC)
            //new int[]{0, 1, 2, 3, 6, 7, 4, 5, 16, 17}, // 10CH: 7.1.2 (out-of-order Cavern DCP) (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR)
            //new int[]{0, 1, 2, 3, 6, 7, 4, 5, 16, 17. 21}, // 11CH: 7.1.2.1 (out-of-order Cavern XL DCP) (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR, BS)
            //new int[]{0, 1, 2, 3, 4, 5, 16, 17, 22, 23, 18, 19}, // 12CH: Barco Auro 11.1 (L, R, C, LFE, SL, SR, TFL, TFR, TFC, GV, TSL, TSR)
            //new int[]{0, 1, 2, 3, 6, 7, 22, 4, 5, 16, 17, 18, 19}, // 13CH: 12-Track (L, R, C, LFE, RL, RR, TFC, SL, SR, TFL, TFR, TSL, TSR)
            //new int[]{0, 1, 2, 3, 6, 7, 4, 5, 16, 17, 22, 23, 18, 19}, // 14CH: Barco Auro 13.1 (L, R, C, LFE, RL, RR, SL, SR, TFL, TFR, TFC, GV, TSL, TSR)
            //new int[]{0, 1, 2, 3, 4, 5, 11, 12, 16, 17, 6, 7, 14, 15, 20}, // 15CH: Cavern (L, R, C, LFE, SL, SR, HI, VI, TL, TR, RL, RR, MD, ES, SL)
            //new int[]{0, 1, 2, 3, 4, 5, 11, 12, 16, 17, 6, 7, 14, 15, 20, 21}, // 16CH: Cavern XL (L, R, C, LFE, SL, SR, HI, VI, TL, TR, RL, RR, MD, ES, SL, BS)
        };
    }
}