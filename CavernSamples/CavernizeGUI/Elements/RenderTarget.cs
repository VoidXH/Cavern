using Cavern;
using Cavern.Channels;
using Cavern.Utilities;

using CavernizeGUI.Resources;

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
        /// The <see cref="Channels"/> are used for rendering, but it could be rematrixed.
        /// This is the number of channels actually written to the file.
        /// </summary>
        public int OutputChannels { get; protected set; }

        /// <summary>
        /// Standard rendering channel layouts.
        /// </summary>
        public RenderTarget(string name, ReferenceChannel[] channels) {
            Name = name;
            Channels = channels;
            OutputChannels = channels.Length;
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

            if (Settings.Default.surroundSwap) {
                for (int i = 0; i < Channels.Length; i++) {
                    if (Channels[i] == ReferenceChannel.SideLeft) {
                        for (int j = 0; j < Channels.Length; j++) {
                            if (Channels[j] == ReferenceChannel.RearLeft) {
                                (systemChannels[i], systemChannels[j]) = (systemChannels[j], systemChannels[i]);
                                (systemChannels[i + 1], systemChannels[j + 1]) = (systemChannels[j + 1], systemChannels[i + 1]);
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            Listener.HeadphoneVirtualizer = false;
            Listener.ReplaceChannels(systemChannels);
        }

        /// <summary>
        /// Top rear channels are used as &quot;side&quot; channels as no true rears are available in standard mappings.
        /// These have to be mapped back to sides in some cases, for example, for the wiring popup.
        /// </summary>
        public ReferenceChannel[] GetNameMappedChannels() {
            ReferenceChannel[] result = Channels.FastClone();
            bool side = false, rear = false;
            for (int i = 0; i < result.Length; i++) {
                side |= result[i] == ReferenceChannel.TopSideLeft;
                rear |= result[i] == ReferenceChannel.TopRearLeft;
            }
            if (side && rear) {
                return result;
            }

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
        /// Gets if a channel is actually present in the final file or just used for downmixing in <see cref="DownmixedRenderTarget"/>.
        /// </summary>
        public virtual bool IsExported(int _) => true;

        /// <summary>
        /// Return the <see cref="Name"/> on string conversion.
        /// </summary>
        override public string ToString() => Name;

        /// <summary>
        /// Default render targets.
        /// </summary>
        /// <remarks>Top rears are used instead of sides for smooth height transitions and WAVEFORMATEXTENSIBLE support.</remarks>
        public static readonly RenderTarget[] Targets = {
            new RenderTarget("4.1.1", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearCenter, ReferenceChannel.TopFrontCenter
            }),
            new RenderTarget("5.1 side", ChannelPrototype.ref510),
            new RenderTarget("5.1 rear", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight
            }),
            new DownmixedRenderTarget("5.1.2 front", ChannelPrototype.ref514, (8, 4), (9, 5)),
            new RenderTarget("5.1.2 side", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,  ReferenceChannel.ScreenLFE,
                ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            }),
            new RenderTarget("5.1.4", ChannelPrototype.ref514),
            new RenderTarget("5.1.6 with top sides", ChannelPrototype.ref516),
            new RenderTarget("5.1.6 for WAVE", ChannelPrototype.wav516),
            new RenderTarget("7.1", ChannelPrototype.ref710),
            new DownmixedRenderTarget("7.1.2 front", ChannelPrototype.ref714, (10, 4), (11, 5)),
            new RenderTarget("7.1.2 side", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            }),
            new RenderTarget("7.1.4", ChannelPrototype.ref714),
            new RenderTarget("7.1.6 with top sides", ChannelPrototype.ref716),
            new RenderTarget("7.1.6 for WAVE", ChannelPrototype.wav716),
            new RenderTarget("9.1", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight
            }),
            new DownmixedRenderTarget("9.1.2 front", ChannelPrototype.ref914, (12, 4), (13, 5)),
            new RenderTarget("9.1.2 side", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
                ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
                ReferenceChannel.WideLeft, ReferenceChannel.WideRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
            }),
            new RenderTarget("9.1.4", ChannelPrototype.ref914),
            new RenderTarget("9.1.6 with top sides", ChannelPrototype.ref916),
            new RenderTarget("9.1.6 for WAVE", ChannelPrototype.wav916),
            new DriverRenderTarget(),
            new VirtualizerRenderTarget()
        };
    }
}