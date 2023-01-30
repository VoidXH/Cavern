using Cavern.Channels;

namespace WAVChannelReorderer {
    partial class MainWindow {
        /// <summary>
        /// Channel layouts for Dune branded media players from 6 to 8 channels.
        /// </summary>
        static readonly ReferenceChannel[][] duneLayouts = {
            // 6CH: 5.1 (L, R, C, SL, SR, LFE)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.ScreenLFE
            },
            // 7CH: 7.0 (L, R, C, SL, RL, RR, SR)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideRight
            },
            // 8CH: 7.1 (L, R, C, SL, RL, RR, SR, LFE)
            new ReferenceChannel[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.SideLeft, ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideRight,
                ReferenceChannel.ScreenLFE
            },
        };
    }
}