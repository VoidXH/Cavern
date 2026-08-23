using System.Numerics;

using Cavern;
using Cavern.Channels;
using Cavern.Listeners;
using Cavern.QuickEQ.SignalGeneration;

using Test.Cavern.Consts;

namespace Test.Cavern {
    /// <summary>
    /// Tests the <see cref="ConvolvedListener"/> class.
    /// </summary>
    [TestClass]
    public class ConvolvedListener_Tests {
        /// <summary>
        /// Tests if <see cref="ConvolvedListener"/> can handle a multi-block Dirac-delta.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void MultiblockDiracDelta() => CavernAmpTest.Run(() => {
            Listener.ReplaceChannels(ChannelPrototype.ToLayout(ChannelPrototype.ref200));
            ConvolvedListener listener = new ConvolvedListener(false);
            const int signalLength = 1024;
            float[][] testSignals = [
                Generators.DiracDelta(signalLength),
                Generators.DiracDelta(signalLength)
            ];
            listener.ConvolutionClip = new(new(testSignals), listener.SampleRate);

            float[] signal = WaveformGenerator.Sine(1, signalLength);
            listener.AttachSource(new() {
                Position = Vector3.UnitX * 10,
                VolumeRolloff = Rolloffs.Disabled,
                Clip = new(new(signal), listener.SampleRate)
            });
            float[] render = listener.Render(3);
            TestUtils.AssertChannel(signal, render, 1, 2, Constants.delta);
        });
    }
}
