using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    partial class EnhancedAC3Decoder {
        /// <summary>
        /// Supported decoder versions.
        /// </summary>
        enum Decoders {
            AlternateAC3 = 6,
            AC3 = 8,
            EAC3 = 16
        }

        /// <summary>
        /// Meaning of values for chexpstr[ch], cplexpstr, and lfeexpstr.
        /// </summary>
        enum ExpStrat {
            Reuse = 0,
            D15,
            D25,
            D45
        }

        /// <summary>
        /// Types of programs in a single frame of a stream.
        /// </summary>
        enum StreamTypes {
            /// <summary>
            /// Main program, can be decoded on its own.
            /// </summary>
            Independent,
            /// <summary>
            /// Should be decoded with the associated independent substream.
            /// </summary>
            Dependent,
            /// <summary>
            /// This frame was converted from AC-3, the E-AC-3 extra data will follow.
            /// Usually used to go beyond 5.1, up to 16 discrete channels.
            /// </summary>
            Repackaged,
            /// <summary>
            /// Unused type.
            /// </summary>
            Reserved
        }

        /// <summary>
        /// First word of all AC-3 frames.
        /// </summary>
        const int syncWord = 0x0B77;

        /// <summary>
        /// Bytes that must be read before determining the frame size.
        /// </summary>
        const int mustDecode = 6;

        /// <summary>
        /// Number of LFE groups.
        /// </summary>
        const int nlfegrps = 2;

        /// <summary>
        /// Fixed LFE mantissa count.
        /// </summary>
        const int nlfemant = 7;

        /// <summary>
        /// Number of blocks per sync frame for each code.
        /// </summary>
        static readonly byte[] numberOfBlocks = new byte[] { 1, 2, 3, 6 };

        /// <summary>
        /// Sample rates for each sample rate code.
        /// </summary>
        static readonly ushort[] sampleRates = new ushort[] { 48000, 44100, 32000 };

        /// <summary>
        /// Frame size code to actual frame size in bytes for 48 kHz sample rate.
        /// For 44.1 kHz, frame sizes are 1393/1280 times these values.
        /// For 32 kHz, frame sizes are 3/2 times these values.
        /// </summary>
        static readonly ushort[] frameSizes = new ushort[19]
            { 128, 160, 192, 224, 256, 320, 384, 448, 512, 640, 768, 896, 1024, 1280, 1536, 1792, 2048, 2304, 2560 };

        /// <summary>
        /// Possible channel arrangements in E-AC-3. The index is the ID read from the file. LFE channel is marked separately.
        /// </summary>
        static readonly ReferenceChannel[][] channelArrangements = {
            new ReferenceChannel[] // 0: dual mono
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontCenter },
            new ReferenceChannel[] // 1: mono
                { ReferenceChannel.FrontCenter },
            new ReferenceChannel[] // 2: stereo
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            new ReferenceChannel[] // 3: 3.x (L, C, R)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontCenter, ReferenceChannel.FrontRight },
            new ReferenceChannel[] // 4: 3.x (L, R, S)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.RearCenter },
            new ReferenceChannel[] // 5: 4.x (L, C, R, S)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontCenter, ReferenceChannel.FrontRight,
                    ReferenceChannel.RearCenter },
            new ReferenceChannel[] // 6: 4.x (L, R, SL, SR)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 9: 5.x (L, C, R, SL, SR)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontCenter, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight }
        };

        /// <summary>
        /// If a custom channel mapping is present, these are the channels for each bit.
        /// </summary>
        static readonly ReferenceChannel[][] channelMappingTargets = {
            new ReferenceChannel[] { ReferenceChannel.ScreenLFE },
            new ReferenceChannel[] { ReferenceChannel.ScreenLFE },
            new ReferenceChannel[] { ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight },
            new ReferenceChannel[] { ReferenceChannel.Unused }, // front height center
            new ReferenceChannel[] { ReferenceChannel.Unused, ReferenceChannel.Unused }, // front height LR
            new ReferenceChannel[] { ReferenceChannel.WideLeft, ReferenceChannel.WideRight },
            new ReferenceChannel[] { ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] { ReferenceChannel.GodsVoice },
            new ReferenceChannel[] { ReferenceChannel.RearCenter },
            new ReferenceChannel[] { ReferenceChannel.RearLeft, ReferenceChannel.RearRight },
            new ReferenceChannel[] { ReferenceChannel.FrontLeftCenter, ReferenceChannel.FrontRightCenter },
            new ReferenceChannel[] { ReferenceChannel.SideRight },
            new ReferenceChannel[] { ReferenceChannel.SideLeft },
            new ReferenceChannel[] { ReferenceChannel.FrontRight },
            new ReferenceChannel[] { ReferenceChannel.FrontCenter },
            new ReferenceChannel[] { ReferenceChannel.FrontLeft }
        };

        /// <summary>
        /// Sub-band transform start coefficients.
        /// </summary>
        static readonly int[] ecplsubbndtab = new int[23]
            { 13, 19, 25, 31, 37, 49, 61, 73, 85, 97, 109, 121, 133, 145, 157, 169, 181, 193, 205, 217, 229, 241, 253 };

        /// <summary>
        /// Frame exponent strategy combinations.
        /// </summary>
        static readonly ExpStrat[][] frmcplexpstr_tbl = new ExpStrat[32][] {
            new ExpStrat[6] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45},
        };
    }
}