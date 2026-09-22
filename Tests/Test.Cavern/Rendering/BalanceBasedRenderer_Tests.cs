using System.Numerics;

using Cavern;
using Cavern.Channels;
using Cavern.Rendering;

using Test.Cavern.Consts;

namespace Test.Cavern.Rendering;

/// <summary>
/// Tests exact speaker routing and gain continuity in the balance-based renderer.
/// </summary>
[TestClass]
public class BalanceBasedRenderer_Tests {
    /// <summary>
    /// Exact placements must preserve the panner's output power without leaking to other speakers.
    /// </summary>
    [DataTestMethod]
    [DataRow(0f)]
    [DataRow(.25f)]
    [DataRow(.5f)]
    [DataRow(1f)]
    [DataRow(2f)]
    public void ExactSpeakerPreservesGainAndIsolation(float gain) {
        Channel[] oldChannels = Listener.Channels;
        Vector3 oldSize = Listener.EnvironmentSize;
        bool oldVirtualizer = Listener.HeadphoneVirtualizer;
        try {
            Listener.HeadphoneVirtualizer = false;
            Listener.EnvironmentSize = new Vector3(10, 7, 10);
            Channel[] channels = ChannelPrototype.ToLayout(ChannelPrototype.ref710);
            Listener.ReplaceChannels(channels);
            Listener listener = new(false) { LFESeparation = true };
            BalanceBasedRenderer renderer = new();
            for (int target = 0; target < channels.Length; target++) {
                if (channels[target].LFE) {
                    continue;
                }
                float[] output = new float[channels.Length];
                renderer.Render(listener, new Source(), channels[target].CubicalPos * Listener.EnvironmentSize,
                    [1], output, gain);
                for (int channel = 0; channel < channels.Length; channel++) {
                    if (channel == target) {
                        Assert.AreEqual(MathF.Sqrt(gain), output[channel], Constants.delta);
                    } else {
                        Assert.AreEqual(0f, output[channel], $"Speaker {target} leaked into speaker {channel}.");
                    }
                }
            }
        } finally {
            Listener.EnvironmentSize = oldSize;
            Listener.HeadphoneVirtualizer = oldVirtualizer;
            Listener.ReplaceChannels(oldChannels);
        }
    }

    /// <summary>
    /// Moving out of the exact-placement tolerance must not change total output power.
    /// </summary>
    [DataTestMethod]
    [DataRow(.25f)]
    [DataRow(.5f)]
    [DataRow(2f)]
    public void MovingOffSpeakerPreservesPower(float gain) {
        Channel[] oldChannels = Listener.Channels;
        Vector3 oldSize = Listener.EnvironmentSize;
        bool oldVirtualizer = Listener.HeadphoneVirtualizer;
        try {
            Listener.HeadphoneVirtualizer = false;
            Listener.EnvironmentSize = new Vector3(10, 7, 10);
            Channel[] channels = ChannelPrototype.ToLayout(ChannelPrototype.ref200);
            Listener.ReplaceChannels(channels);
            Listener listener = new(false) { LFESeparation = true };
            BalanceBasedRenderer renderer = new();
            Vector3 position = channels[0].CubicalPos * Listener.EnvironmentSize;
            float[] exact = new float[channels.Length], nearby = new float[channels.Length];
            renderer.Render(listener, new Source(), position, [1], exact, gain);
            renderer.Render(listener, new Source(), position + new Vector3(.001f, 0, 0), [1], nearby, gain);
            Assert.IsTrue(nearby[1] > 0, "The nearby source must exercise normal panning.");
            float exactPower = exact.Sum(sample => sample * sample);
            float nearbyPower = nearby.Sum(sample => sample * sample);
            Assert.AreEqual(nearbyPower, exactPower, Constants.delta);
        } finally {
            Listener.EnvironmentSize = oldSize;
            Listener.HeadphoneVirtualizer = oldVirtualizer;
            Listener.ReplaceChannels(oldChannels);
        }
    }
}
