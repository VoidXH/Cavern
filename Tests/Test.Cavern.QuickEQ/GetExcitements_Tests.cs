using Cavern.QuickEQ;
using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Measurement;
using Cavern.Utilities;

namespace Test.Cavern.QuickEQ {
    /// <summary>
    /// Tests the <see cref="MeasuredPositionExtensions.GetExcitements"/> method.
    /// </summary>
    [TestClass]
    public class GetExcitements_Tests {
        /// <summary>
        /// Tests if GetExcitements does not modify the input measurements.
        /// </summary>
        [TestMethod, Timeout(20000)]
        public void GetExcitements_DoesNotModifyMeasurements() {
            // Arrange: create a measurement with known impulse response data
            int sampleCount = 1024;
            float[] impulseData = new float[sampleCount];
            impulseData[10] = 1.0f; // Single impulse at sample 10
            Complex[] originalComplex = Complex.Parse(impulseData);
            Complex[] originalComplexCopy = originalComplex.FastClone();

            VerboseImpulseResponse[] impulseResponses = [
                new VerboseImpulseResponse(originalComplex)
            ];
            Equalizer[] frequencyResponses = [new Equalizer()];
            MeasuredPosition measuredPosition = new MeasuredPosition(0, frequencyResponses, impulseResponses);
            IReadOnlyList<MeasuredPosition> measurements = [measuredPosition];

            float[] measurementSignal = new float[sampleCount];
            measurementSignal[10] = 1.0f;

            // Act: call GetExcitements
            measurements.GetExcitements(0, measurementSignal, multithreaded: false);

            // Assert: verify the original ComplexResponse array was not modified
            for (int i = 0; i < originalComplex.Length; i++) {
                Assert.AreEqual(originalComplexCopy[i].Real, originalComplex[i].Real, 1e-10, $"ComplexResponse[{i}].Real was modified by GetExcitements");
                Assert.AreEqual(originalComplexCopy[i].Imaginary, originalComplex[i].Imaginary, 1e-10, $"ComplexResponse[{i}].Imaginary was modified by GetExcitements");
            }
        }

        /// <summary>
        /// Tests if GetExcitements does not modify the input measurements when using multiple threads.
        /// </summary>
        [TestMethod, Timeout(20000)]
        public void GetExcitements_DoesNotModifyMeasurements_Multithreaded() {
            // Arrange
            int sampleCount = 1024;
            float[] impulseData = new float[sampleCount];
            impulseData[10] = 1.0f;
            Complex[] originalComplex = Complex.Parse(impulseData);
            Complex[] originalComplexCopy = originalComplex.FastClone();

            VerboseImpulseResponse[] impulseResponses = [
                new VerboseImpulseResponse(originalComplex)
            ];
            Equalizer[] frequencyResponses = [new Equalizer()];
            MeasuredPosition measuredPosition = new MeasuredPosition(0, frequencyResponses, impulseResponses);
            IReadOnlyList<MeasuredPosition> measurements = [measuredPosition];

            float[] measurementSignal = new float[sampleCount];
            measurementSignal[10] = 1.0f;

            // Act
            measurements.GetExcitements(0, measurementSignal, multithreaded: true);

            // Assert
            for (int i = 0; i < originalComplex.Length; i++) {
                Assert.AreEqual(originalComplexCopy[i].Real, originalComplex[i].Real, 1e-10, $"ComplexResponse[{i}].Real was modified by GetExcitements (multithreaded)");
                Assert.AreEqual(originalComplexCopy[i].Imaginary, originalComplex[i].Imaginary, 1e-10,
                    $"ComplexResponse[{i}].Imaginary was modified by GetExcitements (multithreaded)");
            }
        }

        /// <summary>
        /// Tests if GetExcitements does not modify measurements across multiple positions.
        /// </summary>
        [TestMethod, Timeout(20000)]
        public void GetExcitements_DoesNotModifyMultipleMeasurements() {
            // Arrange
            int sampleCount = 1024;
            int channelCount = 2;
            int positionCount = 3;

            MeasuredPosition[] positions = new MeasuredPosition[positionCount];
            Complex[][][] originalCopies = new Complex[positionCount][][];

            for (int p = 0; p < positionCount; p++) {
                float[] impulseData = new float[sampleCount];
                impulseData[10 + p] = 1.0f;
                VerboseImpulseResponse[] impulseResponses = new VerboseImpulseResponse[channelCount];
                Equalizer[] frequencyResponses = new Equalizer[channelCount];
                originalCopies[p] = new Complex[channelCount][];
                for (int c = 0; c < channelCount; c++) {
                    Complex[] complex = Complex.Parse(impulseData);
                    originalCopies[p][c] = complex.FastClone();
                    impulseResponses[c] = new VerboseImpulseResponse(complex);
                    frequencyResponses[c] = new Equalizer();
                }
                positions[p] = new MeasuredPosition(p, frequencyResponses, impulseResponses);
            }

            IReadOnlyList<MeasuredPosition> measurements = positions;
            float[] measurementSignal = new float[sampleCount];
            measurementSignal[10] = 1.0f;

            // Act
            measurements.GetExcitements(0, measurementSignal, multithreaded: false);

            // Assert
            for (int p = 0; p < positionCount; p++) {
                for (int c = 0; c < channelCount; c++) {
                    for (int i = 0; i < originalCopies[p][c].Length; i++) {
                        Assert.AreEqual(originalCopies[p][c][i].Real, positions[p].ImpulseResponses[c].ComplexResponse[i].Real, 1e-10,
                            $"Position[{p}].ImpulseResponses[{c}].ComplexResponse[{i}].Real was modified");
                        Assert.AreEqual(originalCopies[p][c][i].Imaginary, positions[p].ImpulseResponses[c].ComplexResponse[i].Imaginary, 1e-10,
                            $"Position[{p}].ImpulseResponses[{c}].ComplexResponse[{i}].Imaginary was modified");
                    }
                }
            }
        }
    }
}
