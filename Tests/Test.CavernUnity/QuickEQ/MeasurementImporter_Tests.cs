using Cavern;
using Cavern.Format;
using Cavern.QuickEQ;

namespace Test.Cavern.QuickEQ {
    /// <summary>
    /// Tests the <see cref="MeasurementImporter"/> class.
    /// </summary>
    [TestClass]
    public class MeasurementImporter_Tests {
        /// <summary>
        /// Blocks execution while the import is running.
        /// </summary>
        ManualResetEvent blocker;

        /// <summary>
        /// Tests if a manual test file is imported.
        /// </summary>
        /// <remarks>This is a test for debugging purposes, run it with a custom file path if you'd like to test the importer.</remarks>
        //[TestMethod, Timeout(10000)]
        public void LoadTestFile() {
            const string testFile = "B:\\Downloads\\test.wav";
            AudioReader reader = AudioReader.Open(testFile);
            MultichannelWaveform data = new MultichannelWaveform(reader.ReadMultichannel());
            blocker = new ManualResetEvent(false);
            MeasurementImporter importer = new(data, reader.SampleRate, null);
            importer.OnMeasurement += AfterTest;
            blocker.WaitOne();
        }

        /// <summary>
        /// Sets the <see cref="blocker"/> when the import has finished.
        /// </summary>
        void AfterTest(int measurement, int measurements) {
            if (measurement == measurements - 1) {
                blocker.Set();
            }
        }
    }
}