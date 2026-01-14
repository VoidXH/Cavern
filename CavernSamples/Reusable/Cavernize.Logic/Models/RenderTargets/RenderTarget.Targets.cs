using Cavern.Channels;

namespace Cavernize.Logic.Models.RenderTargets; 
partial class RenderTarget {
    /// <summary>
    /// 5.1.2 side layout with WAVEFORMATEXTENSIBLE channel mask support.
    /// </summary>
    static readonly ReferenceChannel[] side512 = [
        ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,  ReferenceChannel.ScreenLFE,
        ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
    ];

    /// <summary>
    /// 7.1.2 side layout with WAVEFORMATEXTENSIBLE channel mask support.
    /// </summary>
    static readonly ReferenceChannel[] side712 = [
        ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
        ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
        ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
    ];

    /// <summary>
    /// Default render targets.
    /// </summary>
    /// <remarks>Top rears are used instead of sides for smooth height transitions and WAVEFORMATEXTENSIBLE support.</remarks>
    public static readonly RenderTarget[] Targets = [
        new RenderTarget("3.1.2", ChannelPrototype.ref312),
        new RenderTarget("4.0.4", ChannelPrototype.ref404),
        new RenderTarget("4.1.1", ChannelPrototype.ref411),
        new RenderTarget("4.1.3", ChannelPrototype.ref413),
        new RenderTarget("5.1 side", ChannelPrototype.ref510),
        new RenderTarget("5.1 rear", [
            ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
            ReferenceChannel.RearLeft, ReferenceChannel.RearRight
        ]),
        new RenderTarget("5.1.2 side", side512),
        new DownmixedRenderTarget("5.1.2 front", ChannelPrototype.ref514, (8, 4), (9, 5)),
        new DownmixedRenderTarget("5.1.2 matrix", side512, (6, 0), (~6, 4), (7, 1), (~7, 5)),
        new RenderTarget("5.1.4", ChannelPrototype.ref514),
        new DownmixedRenderTarget("5.1.4 matrix", ChannelPrototype.ref514, (8, 0), (~8, 4), (9, 1), (~9, 5)),
        new RenderTarget("5.1.6 with top sides", ChannelPrototype.ref516),
        new RenderTarget("5.1.6 for WAVE", ChannelPrototype.wav516),
        new RenderTarget("7.1", ChannelPrototype.ref710),
        new RenderTarget("7.1.2 side", side712),
        new DownmixedRenderTarget("7.1.2 front", ChannelPrototype.ref714, (10, 4), (11, 5)),
        new DownmixedRenderTarget("7.1.2 matrix", side712, (8, 6), (~8, 4), (9, 7), (~9, 5)),
        new DownmixedRenderTarget("7.1.3 matrix", [
            ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
            ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
            ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight, ReferenceChannel.TopFrontCenter
        ], (8, 6), (~8, 4), (9, 7), (~9, 5), (10, 0), (~10, 1)),
        new RenderTarget("7.1.4", ChannelPrototype.ref714),
        new RenderTarget("7.1.6 with top sides", ChannelPrototype.ref716),
        new RenderTarget("7.1.6 for WAVE", ChannelPrototype.wav716),
        new RenderTarget("9.1", ChannelPrototype.ref910),
        new RenderTarget("9.1.2 side", [
            ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
            ReferenceChannel.RearLeft, ReferenceChannel.RearRight, ReferenceChannel.SideLeft, ReferenceChannel.SideRight,
            ReferenceChannel.WideLeft, ReferenceChannel.WideRight, ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight
        ]),
        new DownmixedRenderTarget("9.1.2 front", ChannelPrototype.ref914, (12, 4), (13, 5)),
        new RenderTarget("9.1.4", ChannelPrototype.ref914),
        new RenderTarget("9.1.6 with top sides", ChannelPrototype.ref916),
        new RenderTarget("9.1.6 for WAVE", ChannelPrototype.wav916),
        new DriverRenderTarget(),
        new VirtualizerRenderTarget()
    ];
}
