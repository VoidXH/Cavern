using System.Numerics;
using System.Reflection;
using Cavern;
using Cavern.Format;

namespace Test.Cavern.Format;

[TestClass]
public class ObjectAudioMetadataEmpty_Tests {
    [TestMethod]
    public void EmptyElementsKeepObjectPositionAndApplyBedAssignment() {
        Type type = typeof(EnhancedAC3Reader).Assembly.GetType("Cavern.Format.Decoders.EnhancedAC3.ObjectAudioMetadata", true);
        object metadata = Activator.CreateInstance(type);
        bool[][] assignment = { new bool[17] };
        assignment[0][3] = true;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        type.GetField("bedAssignment", flags).SetValue(metadata, assignment);
        type.GetProperty("Beds").SetValue(metadata, (byte)1);
        type.GetProperty("ObjectCount").SetValue(metadata, 2);
        Vector3 center = new Vector3(0, 0, 10);
        Source[] sources = { new Source(), new Source { Position = center } };
        foreach (int timecode in new[] { 0, 64, 1535 }) {
            type.GetMethod("UpdateSources").Invoke(metadata, new object[] { timecode, sources });
            Assert.IsTrue(sources[0].LFE);
            Assert.AreEqual(center, sources[1].Position);
        }
    }
}
