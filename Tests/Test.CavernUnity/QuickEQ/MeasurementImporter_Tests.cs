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
        /// Tests if a manual test file is imported.
        /// </summary>
        /// <remarks>This is a test for debugging purposes, run it with a custom file path if you'd like to test the importer.</remarks>
        //[TestMethod, Timeout(10000)]
        public void LoadTestFile() {
            const string testFile = "B:\\Downloads\\test.wav";
            AudioReader reader = AudioReader.Open(testFile);
            var data = new MultichannelWaveform(reader.ReadMultichannel());
            MeasurementImporter importer = new(data, reader.SampleRate, null);
            while (importer.Status != MeasurementImporterStatus.Done) ;
        }
    }
}