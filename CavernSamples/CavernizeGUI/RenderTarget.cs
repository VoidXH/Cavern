﻿using Cavern.Remapping;

namespace CavernizeGUI {
    /// <summary>
    /// Standard rendering channel layouts.
    /// </summary>
    public class RenderTarget {
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
        /// Return the <see cref="Name"/> on string conversion.
        /// </summary>
        override public string ToString() => Name;

        /// <summary>
        /// Default render targets.
        /// </summary>
        public static readonly RenderTarget[] Targets = {
            new RenderTarget("5.1 side", ChannelPrototype.StandardMatrix[6]),
            new RenderTarget("5.1 rear", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight
            }),
            new RenderTarget("5.1.2 front", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight
            }),
            new RenderTarget("5.1.2 side", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            }),
            new RenderTarget("5.1.4", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight
            }),
            new RenderTarget("5.1.6", new[] {
                ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter,
                ReferenceChannel.ScreenLFE, ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
                ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight, ReferenceChannel.TopFrontCenter,
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight, ReferenceChannel.GodsVoice
            }),
            new RenderTarget("7.1", ChannelPrototype.StandardMatrix[8]),
            new RenderTarget("7.1.2 front", ChannelPrototype.StandardMatrix[10]),
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
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight, ReferenceChannel.GodsVoice
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
                ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight, ReferenceChannel.GodsVoice
            })
        };
    }
}