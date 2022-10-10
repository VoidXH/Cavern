using Cavern;
using Cavern.Format.Renderers;
using Cavern.Remapping;

namespace CavernizeGUI {
    /// <summary>
    /// Standard rendering channel layouts.
    /// </summary>
    class RenderTarget {
        /// <summary>
        /// Layout name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// List of used channels.
        /// </summary>
        public ReferenceChannel[] Channels { get; }

        /// <summary>
        /// Standard rendering channel layouts.
        /// </summary>
        public RenderTarget(string name, ReferenceChannel[] channels) {
            Name = name;
            Channels = channels;
        }

        /// <summary>
        /// Apply this render target on the system's output.
        /// </summary>
        public void Apply() {
            Channel[] systemChannels = new Channel[Channels.Length];
            for (int ch = 0; ch < Channels.Length; ++ch) {
                bool lfe = Channels[ch] == ReferenceChannel.ScreenLFE;
                systemChannels[ch] = new Channel(Renderer.channelPositions[(int)Channels[ch]], lfe);
            }
            Listener.ReplaceChannels(systemChannels);
        }

        /// <summary>
        /// Return the <see cref="Name"/> on string conversion.
        /// </summary>
        override public string ToString() => Name;

        /// <summary>
        /// Default render targets.
        /// </summary>
        public static readonly RenderTarget[] Targets = {
            new RenderTarget("5.1 side", ChannelPrototype.GetStandardMatrix(6)),
            new RenderTarget("5.1 rear", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight
            }),
            new RenderTarget("5.1.2 front", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight
            }),
            new RenderTarget("5.1.2 side", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            }),
            new RenderTarget("5.1.4", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            }),
            new RenderTarget("5.1.6", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopFrontCenter,
                ReferenceChannel.GodsVoice, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            }),
            new RenderTarget("7.1", ChannelPrototype.GetStandardMatrix(8)),
            new RenderTarget("7.1.2 front", ChannelPrototype.GetStandardMatrix(10)),
            new RenderTarget("7.1.2 side", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            }),
            new RenderTarget("7.1.4", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            }),
            new RenderTarget("7.1.6", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopFrontCenter,
                ReferenceChannel.GodsVoice, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            }),
            new RenderTarget("9.1", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight
            }),
            new RenderTarget("9.1.2 front", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight
            }),
            new RenderTarget("9.1.2 side", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            }),
            new RenderTarget("9.1.4", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            }),
            new RenderTarget("9.1.6", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopFrontCenter,
                ReferenceChannel.GodsVoice, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            })
        };
    }
}