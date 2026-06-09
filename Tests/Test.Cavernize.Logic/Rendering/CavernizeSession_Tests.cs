using Cavern.Format;
using Cavern.Format.Common;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;

namespace Test.Cavernize.Logic.Rendering;

/// <summary>
/// Tests for the UI-neutral Cavernize render session.
/// </summary>
[TestClass]
public class CavernizeSession_Tests {
    /// <summary>
    /// Render an existing E-AC-3 fixture through the shared conversion service.
    /// </summary>
    [TestMethod, Timeout(10000)]
    public void RenderPcmWave() {
        string source = Path.Combine(Consts.cavernFormatTestData, "E-AC-3 in.mkv"),
            output = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        try {
            using CavernizeSession session = new() {
                RenderTarget = RenderTarget.Targets.First(target => target.Name == "2.0")
            };

            session.OpenContent(source);
            Assert.AreEqual(Codec.EnhancedAC3, session.SelectedTrack.Codec);

            session.RenderContent(output);

            using AudioReader reader = AudioReader.Open(output);
            float[][] rendered = reader.ReadMultichannel();
            Assert.AreEqual(2, rendered.Length);
            Assert.AreEqual(48000, reader.SampleRate);
            Assert.IsTrue(reader.Length > 0);
            Assert.IsTrue(rendered[0].Length > 0);
        } finally {
            File.Delete(output);
        }
    }
}
