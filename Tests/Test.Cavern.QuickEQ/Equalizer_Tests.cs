using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Test.Cavern {
    /// <summary>
    /// Tests the <see cref="Equalizer"/> class.
    /// </summary>
    [TestClass]
    public class Equalizer_Tests {
        /// <summary>
        /// Tests if <see cref="Equalizer.Smooth(double)"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Smooth() {
            Equalizer equalizer = Create(20, 20000, 100, 10);
            equalizer.Smooth(2);
            Assert.IsTrue(equalizer.Bands.All(x => Math.Abs(x.Gain) < 3));
        }

        /// <summary>
        /// Create an <see cref="Equalizer"/> that resembles a sine wave of a given amplitude.
        /// </summary>
        static Equalizer Create(double startFreq, double endFreq, int length, double amplitude) {
            Equalizer result = new Equalizer();
            double mul = 1.0 / (length - 1);
            for (int i = 0; i < length; i++) {
                result.AddBand(new Band(QMath.Lerp(startFreq, endFreq, i * mul), amplitude * Math.Sin(i)));
            }
            return result;
        }
    }
}