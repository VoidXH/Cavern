using Cavern;
using Cavern.Remapping;

namespace CavernizeGUI.Elements {
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
        public virtual void Apply() {
            Channel[] systemChannels = new Channel[Channels.Length];
            for (int ch = 0; ch < Channels.Length; ch++) {
                bool lfe = Channels[ch] == ReferenceChannel.ScreenLFE;
                systemChannels[ch] = new Channel(ChannelPrototype.AlternativePositions[(int)Channels[ch]], lfe);
            }
            Listener.HeadphoneVirtualizer = false;
            Listener.ReplaceChannels(systemChannels);
        }

        /// <summary>
        /// Top rear channels are used as &quot;side&quot; channels as no true rears are available in standard mappings.
        /// These have to be mapped back to sides in some cases, for example, for the wiring popup.
        /// </summary>
        public ReferenceChannel[] GetNameMappedChannels() {
            ReferenceChannel[] result = (ReferenceChannel[])Channels.Clone();
            for (int i = 0; i < result.Length; i++) {
                if (result[i] == ReferenceChannel.TopRearLeft) {
                    result[i] = ReferenceChannel.TopSideLeft;
                }
                if (result[i] == ReferenceChannel.TopRearRight) {
                    result[i] = ReferenceChannel.TopSideRight;
                }
            }
            return result;
        }

        /// <summary>
        /// Return the <see cref="Name"/> on string conversion.
        /// </summary>
        override public string ToString() => Name;

        /// <summary>
        /// Default render targets.
        /// </summary>
        /// <remarks>Top rears are used instead of sides for smooth height transitions and WAVEFORMATEXTENSIBLE support.</remarks>
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
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            }),
            new RenderTarget("5.1.4", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            }),
            new RenderTarget("5.1.6", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight
            }),
            new RenderTarget("7.1", ChannelPrototype.GetStandardMatrix(8)),
            new RenderTarget("7.1.2 front", ChannelPrototype.GetStandardMatrix(10)),
            new RenderTarget("7.1.2 side", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            }),
            new RenderTarget("7.1.4", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            }),
            new RenderTarget("7.1.6", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight
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
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            }),
            new RenderTarget("9.1.4", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            }),
            new RenderTarget("9.1.6", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight
            }),
            new DriverRenderTarget(),
            new VirtualizerRenderTarget()
        };
    }
}