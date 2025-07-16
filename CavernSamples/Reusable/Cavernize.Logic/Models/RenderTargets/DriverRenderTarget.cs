﻿using Cavern;
using Cavern.Channels;

namespace Cavernize.Logic.Models.RenderTargets;

/// <summary>
/// Applies the layout that's set up in the Cavern Driver.
/// </summary>
public class DriverRenderTarget : RenderTarget {
    /// <summary>
    /// Applies the layout that's set up in the Cavern Driver.
    /// </summary>
    public DriverRenderTarget() : base(targetName, GetChannels()) { }

    /// <summary>
    /// Gets the channels set up in the Cavern Driver.
    /// </summary>
    static ReferenceChannel[] GetChannels() {
        new Listener();
        ReferenceChannel[] result = new ReferenceChannel[Listener.Channels.Length];
        for (int i = 0; i < result.Length; i++) {
            result[i] = ChannelPrototype.GetReference(Listener.Channels[i]);
        }
        return result;
    }

    /// <inheritdoc/>
    public override void Apply(bool _) => new Listener();

    /// <summary>
    /// Name of this render target.
    /// </summary>
    const string targetName = "Cavern Driver";
}
