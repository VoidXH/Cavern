using System.Reflection;
using Cavern;
using Cavern.Format;
using Cavern.Format.Utilities;

namespace Test.Cavern.Format;

[TestClass]
public class ConsumerEAC3_Tests {
    static readonly Assembly assembly = typeof(EnhancedAC3Reader).Assembly;
    const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestMethod]
    public void ConsumerHeadersUseEAC3AndRetainWireID() {
        Type type = assembly.GetType("Cavern.Format.Transcoders.EnhancedAC3Header", true);
        foreach (int bsid in new[] { 12, 16 }) {
            foreach (bool dependent in new[] { false, true }) {
                byte[] data = new byte[32];
                int bit = 0;
                void Put(int value, int width) {
                    for (int i = width - 1; i >= 0; i--, bit++) {
                        data[bit >> 3] |= (byte)(((value >> i) & 1) << (7 - (bit & 7)));
                    }
                }
                Put(0x0b77, 16); Put(dependent ? 1 : 0, 2); Put(0, 3); Put(15, 11);
                Put(0, 2); Put(3, 2); Put(7, 3); Put(1, 1); Put(bsid, 5);
                Put(31, 5); Put(0, 1);
                if (dependent) { Put(1, 1); Put(0x0180, 16); }
                Put(0, 1); Put(0, 1); Put(0, 1);
                object header = Activator.CreateInstance(type);
                using var input = new MemoryStream(data);
                type.GetMethod("Decode").Invoke(header, new object[] { BlockBuffer<byte>.Create(input) });
                Assert.AreEqual(16, Convert.ToInt32(type.GetProperty("Decoder").GetValue(header)));
                Assert.AreEqual(bsid, type.GetField("bitstreamID", hidden).GetValue(header));
                Assert.AreEqual(48000, type.GetProperty("SampleRate").GetValue(header));
                Assert.AreEqual(dependent ? 8 : 0, type.GetProperty("SubstreamID").GetValue(header));
                object planter = type.GetMethod("Encode").Invoke(header, null);
                using var output = new MemoryStream();
                planter.GetType().GetMethod("WriteToStream").Invoke(planter, new object[] { output });
                Assert.AreEqual(bsid, output.ToArray()[5] >> 3);
            }
        }
        var parse = type.GetMethod("ParseDecoder", BindingFlags.Static | BindingFlags.NonPublic);
        foreach (int unsupported in new[] { 9, 10, 11, 13, 17, 31 }) {
            var error = Assert.ThrowsException<TargetInvocationException>(() => parse.Invoke(null, new object[] { unsupported }));
            Assert.AreEqual("UnsupportedFeatureException", error.InnerException.GetType().Name);
        }
    }

    // Fixed metadata only, without compressed audio, from an LG consumer encoder.
    const string lgMetadata = "583800900af48080007838644f10b000089ce739ce70804401da404001c046100f27272727272727260b00000000000000000000000000000182c0000000000000000000000000000060b00000000000000000000000000000182c0000000000000000000000000000060b00000000000000000000000000000182c0000000000000000000000000000060b0000009d9fb225fa8";

    [TestMethod]
    public void KeysZeroAndOneDecodeFixed512AndSevenCarrierIdentity() {
        foreach (int key in new[] { 0, 1 }) {
            object decoder = DecodeMetadata(key);
            Type type = decoder.GetType();
            Assert.IsTrue((bool)type.GetProperty("HasObjects").GetValue(decoder));
            object oamd = type.GetProperty("OAMD").GetValue(decoder);
            Type oamdType = oamd.GetType();
            Assert.AreEqual(8, oamdType.GetProperty("ObjectCount").GetValue(oamd));
            Assert.AreEqual((byte)8, oamdType.GetProperty("Beds").GetValue(oamd));
            Assert.AreEqual(1312, oamdType.GetField("offset", hidden).GetValue(oamd));
            var channels = (Array)oamdType.GetMethod("GetStaticChannels").Invoke(oamd, null);
            string[] expected = { "FrontLeft", "FrontRight", "FrontCenter", "ScreenLFE", "SideLeft", "SideRight", "TopSideLeft", "TopSideRight" };
            CollectionAssert.AreEqual(expected, channels.Cast<object>().Select(channel => channel.ToString()).ToArray());
            Source[] sources = Enumerable.Range(0, 8).Select(_ => new Source()).ToArray();
            oamdType.GetMethod("UpdateSources").Invoke(oamd, new object[] { 1312, sources });
            Assert.IsTrue(sources[3].LFE);
            Assert.IsTrue(sources[6].Position.Y > 0 && sources[7].Position.Y > 0);
            using IDisposable joc = (IDisposable)type.GetProperty("JOC").GetValue(decoder);
            Type jocType = joc.GetType();
            Assert.AreEqual(7, jocType.GetProperty("ChannelCount").GetValue(joc));
            Assert.AreEqual(7, jocType.GetProperty("ObjectCount").GetValue(joc));
            var matrix = (float[][][][])jocType.GetMethod("GetMixingMatrices").Invoke(joc, new object[] { 1536 });
            for (int obj = 0; obj < 7; obj++) {
                for (int carrier = 0; carrier < 7; carrier++) {
                    for (int band = 0; band < 64; band++) {
                        Assert.AreEqual(obj == carrier ? 1f : 0f, matrix[obj][23][carrier][band], 1e-5f);
                    }
                }
            }
        }
    }

    [TestMethod]
    public void UnknownKeysAndVersionsRemainUnsupported() {
        foreach (int key in new[] { 2, 3, 4, 5, 6 }) {
            object decoder = DecodeMetadata(key);
            Assert.IsFalse((bool)decoder.GetType().GetProperty("HasObjects").GetValue(decoder));
        }
        object versionOne = DecodeMetadata(1, 1);
        Assert.IsFalse((bool)versionOne.GetType().GetProperty("HasObjects").GetValue(versionOne));
    }

    static object DecodeMetadata(int key, int version = 0) {
        byte[] bytes = Convert.FromHexString(lgMetadata);
        bytes[4] = (byte)((bytes[4] & 7) | (key << 3) | (version << 6));
        Type type = assembly.GetType("Cavern.Format.Decoders.EnhancedAC3.ExtensibleMetadataDecoder", true);
        object decoder = Activator.CreateInstance(type);
        Type extractorType = assembly.GetType("Cavern.Format.Utilities.BitExtractor", true);
        object extractor = Activator.CreateInstance(extractorType, new object[] { bytes });
        type.GetMethod("Decode").Invoke(decoder, new[] { extractor });
        return decoder;
    }
}
