using Cavern;

namespace Test.Cavern {
    /// <summary>
    /// Tests the <see cref="Clip"/> class.
    /// </summary>
    [TestClass]
    public class Clip_Tests {
        /// <summary>
        /// Tests if a mono clip's samples can be assigned and queried with.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void SetGetData_Mono() {
            Clip clip = new Clip(new float[Consts.samples.Length], 1, Listener.DefaultSampleRate);
            Assert.IsTrue(clip.SetData(Consts.samples, loopOffset));

            float[] getTo = new float[Consts.samples.Length];
            Assert.IsTrue(clip.GetData(getTo, loopOffset));
            CollectionAssert.AreEqual(getTo, Consts.samples);

            Assert.IsTrue(clip.GetDataNonLooping(getTo, loopOffset));
            CollectionAssert.AreEqual(getTo[..(Consts.samples.Length - loopOffset)], Consts.samples[..^loopOffset]);
        }

        /// <summary>
        /// Tests if a stereo clip's samples can be assigned and queried with.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void SetGetData_Multichannel() {
            Clip clip = new Clip(new float[][] {
                new float[Consts.samples.Length],
                new float[Consts.samples.Length]
            }, Listener.DefaultSampleRate);
            Assert.IsTrue(clip.SetData(Consts.multichannel, loopOffset));

            float[] getTo = new float[Consts.samples.Length];
            Assert.IsTrue(clip.GetData(getTo, 0, loopOffset));
            CollectionAssert.AreEqual(getTo, Consts.samples);

            float[][] getMultichannelTo = new float[][] { new float[Consts.samples.Length], new float[Consts.samples.Length] };
            Assert.IsTrue(clip.GetData(getMultichannelTo, loopOffset));
            CollectionAssert.AreEqual(getMultichannelTo[0], Consts.samples);

            Assert.IsTrue(clip.GetDataNonLooping(getMultichannelTo, loopOffset));
            CollectionAssert.AreEqual(getMultichannelTo[1][..(Consts.samples.Length - loopOffset)], Consts.samples2[..^loopOffset]);
        }

        /// <summary>
        /// In looping functions, set the offset to this many samples to check if the looping works.
        /// </summary>
        const int loopOffset = 2;
    }
}