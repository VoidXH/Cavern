using Cavern.Channels;

namespace Cavern.Format.Transcoders {
    static partial class EnhancedAC3 {
        /// <summary>
        /// First word of all AC-3 frames.
        /// </summary>
        public const int syncWord = 0x0B77;

        /// <summary>
        /// First word of all AC-3 frames in little-endian order.
        /// </summary>
        public const int syncWordLE = 0x770B;

        /// <summary>
        /// Bytes that must be read before determining the frame size.
        /// </summary>
        public const int mustDecode = 7;

        /// <summary>
        /// Frame size code to actual frame size in bytes for 48 kHz sample rate.
        /// For 44.1 kHz, frame sizes are 1393/1280 times these values.
        /// For 32 kHz, frame sizes are 3/2 times these values.
        /// </summary>
        public static readonly ushort[] frameSizes =
            { 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 448, 512, 640, 768, 896, 1024, 1152, 1280 };

        /// <summary>
        /// Number of blocks per sync frame for each code.
        /// </summary>
        public static readonly byte[] numberOfBlocks = { 1, 2, 3, 6 };

        /// <summary>
        /// Sample rates for each sample rate code.
        /// </summary>
        public static readonly ushort[] sampleRates = { 48000, 44100, 32000 };

        /// <summary>
        /// Possible channel arrangements in E-AC-3. The index is the ID read from the file. LFE channel is marked separately.
        /// </summary>
        public static readonly ReferenceChannel[][] channelArrangements = {
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
        public static readonly ReferenceChannel[][] channelMappingTargets = {
            new ReferenceChannel[] { ReferenceChannel.ScreenLFE },
            new ReferenceChannel[] { ReferenceChannel.ScreenLFE },
            new ReferenceChannel[] { ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight },
            new ReferenceChannel[] { ReferenceChannel.TopFrontCenter },
            new ReferenceChannel[] { ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight },
            new ReferenceChannel[] { ReferenceChannel.WideLeft, ReferenceChannel.WideRight },
            new ReferenceChannel[] { ReferenceChannel.ScreenLFE, ReferenceChannel.ScreenLFE }, // Side surround, but not used
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
    }
}