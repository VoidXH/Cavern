using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    internal partial class EnhancedAC3Decoder {
        /// <summary>
        /// First word of all AC-3 frames.
        /// </summary>
        const int syncWord = 0x0B77;

        /// <summary>
        /// Bytes that must be read before determining the frame size.
        /// </summary>
        const int mustDecode = 5;

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
        /// Possible channel arrangements in E-AC3. The index is the ID read from the file. LFE channel is marked separately.
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
        /// Supported decoder versions.
        /// </summary>
        enum Decoder {
            AC3 = 8,
            EAC3 = 16
        }

        /// <summary>
        /// Meaning of values for chexpstr[ch], cplexpstr, and lfeexpstr.
        /// </summary>
        enum ExponentStrategies {
            Reuse = 0,
            D15,
            D25,
            D45
        }

        /// <summary>
        /// Sub-band transform start coefficients.
        /// </summary>
        static readonly int[] ecplsubbndtab = new int[23]
            { 13, 19, 25, 31, 37, 49, 61, 73, 85, 97, 109, 121, 133, 145, 157, 169, 181, 193, 205, 217, 229, 241, 253 };
    }
}