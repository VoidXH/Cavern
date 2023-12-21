using Cavern;
using Cavern.Channels;
using Cavern.Utilities;

namespace Test.Cavern {
    /// <summary>
    /// Tests the <see cref="WaveformUtils"/> class.
    /// </summary>
    [TestClass]
    public class WaveformUtils_Tests {
        /// <summary>
        /// Tests if <see cref="WaveformUtils.Downmix(float[], int)"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Downmix() {
            float[] downmix = Consts.stereoSamples.Downmix(2);
            Assert.AreEqual(.2f, downmix[0]);
            Assert.AreEqual(.2f, downmix[1]);
            Assert.AreEqual(.4f, downmix[2]);
            Assert.AreEqual(.6f, downmix[3]);
        }

        /// <summary>
        /// Tests if <see cref="WaveformUtils.Downmix(float[], float[], int)"/> works for playing quadro on a 5.1 system.
        /// It's called downmix as it's mostly downmixing, but actually mixes to a channel count without knowledge of their layout.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void DownmixUp() {
            Listener.ReplaceChannels(ChannelPrototype.ToLayout(ChannelPrototype.GetStandardMatrix(4)));
            float[] result = new float[Consts.stereoSamples.Length / 4 * 6];
            WaveformUtils.Downmix(Consts.stereoSamples, result, 6);
            float[] expected = [.1f, .1f, 0, 0, 0, .2f, .1f, .3f, 0, 0, .1f, .5f];
            CollectionAssert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests if <see cref="WaveformUtils.GetPeak(float[])"/> works at any index.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetPeak() {
            float[] source = new float[3];
            for (int i = 0; i < source.Length;) {
                source[i] = i + 1;
                Assert.AreEqual(++i, (int)source.GetPeak());
            }
        }

        /// <summary>
        /// Tests if <see cref="WaveformUtils.TrimEnd(float[][])"/> correctly cuts the end of jagged arrays.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void TrimEnd_2D() {
            MultichannelWaveform source = new(
                new float[100], // Will be cut until the nicest element
                new float[100] // Will be empty, but not cut, since the other jagged array is longer
            );
            source[0][Consts.nice] = 1;
            source.TrimEnd();

            Assert.AreEqual(Consts.nice + 1, source[0].Length);
            Assert.AreEqual(source[0].Length, source[1].Length);
        }
    }
}