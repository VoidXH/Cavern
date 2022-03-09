using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    internal partial class DTSCoherentAcousticsDecoder {
        /// <summary>
        /// Marks the beginning of a new DCA frame.
        /// </summary>
        const int syncWord = 0x7FFE8001;

        /// <summary>
        /// Headers are handled with weird bit size words. This is their sum in bytes.
        /// </summary>
        const int headerLength = 13;

        /// <summary>
        /// Normal frame marker. Sample deficit is not allowed.
        /// </summary>
        const int allowedDeficit = 31;

        /// <summary>
        /// Possible channel arrangements in DTS Core. The index is the ID read from the file. LFE channel is marked separately.
        /// </summary>
        static readonly ReferenceChannel[][] coreChannelArrangements = {
            new ReferenceChannel[] // 0: mono
                { ReferenceChannel.FrontCenter },
            new ReferenceChannel[] // 1: dual mono
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontCenter },
            new ReferenceChannel[] // 2: stereo
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            new ReferenceChannel[] // 3: sum-difference (L+R and L-R)
                { ReferenceChannel.FrontCenter, ReferenceChannel.RearCenter },
            new ReferenceChannel[] // 4: left + right total
                { ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 5: 3.x (C, L, R)
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight },
            new ReferenceChannel[] // 6: 3.x (L, R, S)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.RearCenter },
            new ReferenceChannel[] // 7: 4.x (C, L, R, S)
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.RearCenter },
            new ReferenceChannel[] // 8: 4.x (L, R, SL, SR)
                { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 9: 5.x (C, L, R, SL, SR)
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 10: 6.x (CL, CR, L, R, SL, SR)
                { ReferenceChannel.FrontLeftCenter, ReferenceChannel.FrontRightCenter,
                    ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 11: 6.x (C, L, R, RL, RR, GV)
                { ReferenceChannel.FrontCenter, ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.GodsVoice },
            new ReferenceChannel[] // 12: 6.x (CF, CR, LF, RF, LR, RR)
                { ReferenceChannel.FrontCenter, ReferenceChannel.RearCenter,
                    ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.RearLeft, ReferenceChannel.RearRight },
            new ReferenceChannel[] // 13: 7.x (CL, C, CR, L, R, SL, SR)
                { ReferenceChannel.FrontLeftCenter, ReferenceChannel.FrontCenter, ReferenceChannel.FrontRightCenter,
                    ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 14: 8.x (CL, CR, L, R, SL1, SL2, SR1, SR2)
                { ReferenceChannel.FrontLeftCenter, ReferenceChannel.FrontRightCenter,
                    ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.SideLeft,
                    ReferenceChannel.SideRight, ReferenceChannel.SideRight },
            new ReferenceChannel[] // 15: 8.x (CL, C, CR, L, R, SL, S, SR)
                { ReferenceChannel.FrontLeftCenter, ReferenceChannel.FrontCenter, ReferenceChannel.FrontRightCenter,
                    ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight,
                    ReferenceChannel.SideLeft, ReferenceChannel.RearCenter, ReferenceChannel.SideRight },
        };

        /// <summary>
        /// Possible sample rates. The index is the ID read from the file.
        /// </summary>
        static readonly ushort[] sampleRates =
            new ushort[16] { 0, 8000, 16000, 32000, 0, 0, 11025, 22050, 44100, 0, 0, 12000, 24000, 48000, 0, 0 };
    }
}