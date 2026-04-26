using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Container;

namespace Test.Cavern.Format.Output.Container;

[TestClass]
/// <summary>
/// Tests the <see cref="MatroskaWriter"/> class.
/// </summary>
public class MatroskaWriter_Tests {
    /// <summary>
    /// Tests if <see cref="MatroskaWriter"/> still produces the reference output.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Reference() {
        string input = Path.Combine(Consts.testData, "E-AC-3 in.mkv");
        string output = Path.Combine(Consts.testData, "E-AC-3 out.mkv");
        string temp = Path.Combine(Consts.testData, "E-AC-3 temp.mkv");

        using (MatroskaReader reader = new(input))
        using (AudioTrackReader decoder = new(reader.Tracks[0])) {
            float[] samples = decoder.Read();
            AudioWriterIntoContainer writer =
                new(temp, [], Codec.PCM_LE, (int)decoder.Length, decoder.ChannelCount, decoder.Length, decoder.SampleRate, BitDepth.Int16);
            writer.Write(samples);
        }

        MatroskaComparator.Compare(output, temp);
        File.Delete(temp);
    }
}
