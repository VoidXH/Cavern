using System.Numerics;

using Cavern;
using Cavern.Channels;

using Test.Cavern.Consts;

namespace Test.Cavern;

/// <summary>
/// Tests that a <see cref="Source"/> placed at a <see cref="Channel"/>'s location is rendered from that channel and only that channel,
/// including LFE isolation and more complex multi-source / time-offset cases.
/// </summary>
[TestClass]
public class Listener_Tests {
    /// <summary>
    /// A placement: the target channel, the per-update input signal, and whether the source is LFE-tagged.
    /// </summary>
    readonly record struct ChannelInput(ReferenceChannel Channel, float[] Signal, bool IsLFE = false);

    /// <summary>
    /// A single-sample mono signal placed at <paramref name="offset"/> within one update frame.
    /// </summary>
    static float[] UnitPulse(int offset) {
        float[] pulse = new float[240];
        pulse[offset] = 1;
        return pulse;
    }

    /// <summary>
    /// Renders the given <paramref name="inputs"/> through a real <see cref="Listener"/> (each source placed at its channel's
    /// position, resolved from <see cref="ChannelPrototype"/>), then asserts that every target channel carries exactly its input
    /// signal and every other channel (including LFE) stays completely silent. Crossover is disabled and LFE separation is on.
    /// </summary>
    static void AssertRendersOnlyFrom(params (ReferenceChannel Channel, float[] Signal)[] inputs) =>
        AssertRendersOnlyFrom(Array.ConvertAll(inputs, i => new ChannelInput(i.Channel, i.Signal)));

    /// <summary>
    /// Overload accepting explicit LFE tagging for the LFE isolation case.
    /// </summary>
    static void AssertRendersOnlyFrom((ReferenceChannel Channel, float[] Signal, bool IsLFE) input) =>
        AssertRendersOnlyFrom(new ChannelInput(input.Channel, input.Signal, input.IsLFE));

    /// <summary>
    /// Core tester: runs the placements through a listener and checks the output against the inputs, channel by channel.
    /// Crossover is disabled (<see cref="Listener.DirectLFE"/>), LFE separation is on, distance rolloff is disabled, and the
    /// normalizer is off so that every target channel carries exactly its input signal and every other channel stays silent.
    /// </summary>
    static void AssertRendersOnlyFrom(params ChannelInput[] inputs) {
        ReferenceChannel[] layout = ChannelPrototype.ref712;
        Channel[] channels = ChannelPrototype.ToLayout(layout);
        Vector3[] positions = ChannelPrototype.ToPositions(layout);
        Listener.ReplaceChannels(channels);

        Listener listener = new(false) {
            LFESeparation = true,
            DirectLFE = true
        };

        int updateRate = listener.UpdateRate;
        int channelCount = channels.Length;
        foreach (ChannelInput input in inputs) {
            Clip clip = new Clip(input.Signal, 1, listener.SampleRate);
            Source source = new Source {
                Clip = clip,
                VolumeRolloff = Rolloffs.Disabled,
                LFE = input.IsLFE,
                Position = positions[Array.IndexOf(layout, input.Channel)]
            };
            listener.AttachSource(source);
        }

        float[] output = listener.Render();
        Assert.AreEqual(channelCount * updateRate, output.Length);
        for (int channel = 0; channel < channelCount; channel++) {
            ChannelInput? match = null;
            foreach (ChannelInput input in inputs) {
                if (Array.IndexOf(layout, input.Channel) == channel) {
                    match = input;
                }
            }
            for (int sample = 0; sample < updateRate; sample++) {
                float value = output[channelCount * sample + channel];
                if (match.HasValue) {
                    Assert.AreEqual(match.Value.Signal[sample], value, Constants.delta,
                        $"Channel {channel} ({ChannelPrototype.GetName(channels[channel])}) sample {sample} should match the input.");
                } else {
                    Assert.AreEqual(0f, value, Constants.delta,
                        $"Channel {channel} ({ChannelPrototype.GetName(channels[channel])}) should be silent.");
                }
            }
        }
    }

    /// <summary>
    /// A single source at the front-left channel must sound only from there (LFE stays silent).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SingleSourceRendersOnlyThere() => AssertRendersOnlyFrom((ReferenceChannel.FrontLeft, UnitPulse(37)));

    /// <summary>
    /// A single source at every non-LFE channel must sound only from that exact channel.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void EveryChannelIsolated() {
        ReferenceChannel[] layout = ChannelPrototype.ref712;
        foreach (ReferenceChannel channel in layout) {
            if (channel == ReferenceChannel.ScreenLFE) {
                continue;
            }
            AssertRendersOnlyFrom((channel, UnitPulse(37)));
        }
    }

    /// <summary>
    /// An LFE-tagged source must sound only from the subwoofer(s), nothing else.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void LFESourceRendersOnlyToSubwoofer() => AssertRendersOnlyFrom((ReferenceChannel.ScreenLFE, UnitPulse(37), true));

    /// <summary>
    /// Multiple sources at distinct channels must each sound only from their own channel.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void MultipleSourcesStayIsolated() => AssertRendersOnlyFrom(
            (ReferenceChannel.FrontLeft, UnitPulse(37)),
            (ReferenceChannel.RearRight, UnitPulse(82)),
            (ReferenceChannel.TopFrontLeft, UnitPulse(11))
    );

    /// <summary>
    /// A source with an intra-frame time offset must still sound only from its channel, at the right sample.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SourceWithTimeOffsetRendersOnlyThere() => AssertRendersOnlyFrom((ReferenceChannel.FrontRight, UnitPulse(123)));
}
