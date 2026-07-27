using System.Text;

using Cavern;
using Cavern.Channels;
using Cavern.Format.Exceptions;
using Cavern.Format.Environment.Utilities;
using Cavern.Format.Transcoders;

namespace Test.Cavern.Format.Transcoders;

/// <summary>
/// Tests for <see cref="DolbyAtmosMasterFormatRootFile"/>.
/// </summary>
[TestClass]
public class DolbyAtmosMasterFormatRootFile_Tests {
    /// <summary>
    /// Tests a mixed bed/object export and verifies the written YAML and channel mapping.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Export_MixedSources_WritesExpectedYaml() {
        Source frontLeft = new();
        Source topSideLeft = new();
        Source topSideRight = new();
        Source objectSource = new();

        StaticSource[] staticObjects = [
            new StaticSource(ReferenceChannel.FrontLeft, frontLeft),
            new StaticSource(ReferenceChannel.TopSideLeft, topSideLeft),
            new StaticSource(ReferenceChannel.TopSideRight, topSideRight)
        ];
        Source[] sources = [frontLeft, topSideLeft, topSideRight, objectSource];
        int[] channelIDs = new int[sources.Length];

        DolbyAtmosMasterFormatRootFile root = new(staticObjects, sources, channelIDs);

        Assert.AreEqual(3, root.BedChannelCount);
        CollectionAssert.AreEqual(new[] { ReferenceChannel.FrontLeft, ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight }, root.Channels);
        CollectionAssert.AreEqual(new[] { 0, 8, 9, 10 }, root.ObjectMapping);

        AssertExport(root, BuildExportYaml(
            "scBedConfiguration: [0, 8, 9]",
            [
                "          - channel: L",
                "            ID: 0",
                "          - channel: Lts",
                "            ID: 8",
                "          - channel: Rts",
                "            ID: 9"
            ],
            [
                "      - ID: 10"
            ]));
    }

    /// <summary>
    /// Tests an export that contains only bed channels and no dynamic objects.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Export_BedOnly_WritesEmptyObjectsCollection() {
        Source frontLeft = new();
        Source topSideLeft = new();

        StaticSource[] staticObjects = [
            new StaticSource(ReferenceChannel.FrontLeft, frontLeft),
            new StaticSource(ReferenceChannel.TopSideLeft, topSideLeft)
        ];
        Source[] sources = [frontLeft, topSideLeft];
        int[] channelIDs = new int[sources.Length];

        DolbyAtmosMasterFormatRootFile root = new(staticObjects, sources, channelIDs);

        Assert.AreEqual(2, root.BedChannelCount);
        CollectionAssert.AreEqual(new[] { ReferenceChannel.FrontLeft, ReferenceChannel.TopSideLeft }, root.Channels);
        CollectionAssert.AreEqual(new[] { 0, 8 }, root.ObjectMapping);

        AssertExport(root, BuildExportYaml(
            "scBedConfiguration: [0, 8]",
            [
                "          - channel: L",
                "            ID: 0",
                "          - channel: Lts",
                "            ID: 8"
            ],
            []));
    }

    /// <summary>
    /// Tests an export that contains only dynamic objects and no bed channels.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Export_ObjectOnly_WritesEmptyBedCollection() {
        Source objectA = new();
        Source objectB = new();

        StaticSource[] staticObjects = Array.Empty<StaticSource>();
        Source[] sources = [objectA, objectB];
        int[] channelIDs = new int[sources.Length];

        DolbyAtmosMasterFormatRootFile root = new(staticObjects, sources, channelIDs);

        Assert.AreEqual(0, root.BedChannelCount);
        CollectionAssert.AreEqual(Array.Empty<ReferenceChannel>(), root.Channels);
        CollectionAssert.AreEqual(new[] { 10, 11 }, root.ObjectMapping);

        AssertExport(root, BuildExportYaml(
            "scBedConfiguration: []",
            [],
            [
                "      - ID: 10",
                "      - ID: 11"
            ]));
    }

    /// <summary>
    /// Tests an export with no sources at all.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Export_EmptySources_WritesEmptyCollections() {
        DolbyAtmosMasterFormatRootFile root = new(Array.Empty<StaticSource>(), Array.Empty<Source>(), Array.Empty<int>());

        Assert.AreEqual(0, root.BedChannelCount);
        CollectionAssert.AreEqual(Array.Empty<ReferenceChannel>(), root.Channels);
        CollectionAssert.AreEqual(Array.Empty<int>(), root.ObjectMapping);

        AssertExport(root, BuildExportYaml("scBedConfiguration: []", [], []));
    }

    /// <summary>
    /// Tests parsing an exported mixed root file back into the same in-memory representation.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_RoundTrip_MixedSources() {
        Source frontLeft = new();
        Source topSideLeft = new();
        Source topSideRight = new();
        Source objectSource = new();

        StaticSource[] staticObjects = [
            new StaticSource(ReferenceChannel.FrontLeft, frontLeft),
            new StaticSource(ReferenceChannel.TopSideLeft, topSideLeft),
            new StaticSource(ReferenceChannel.TopSideRight, topSideRight)
        ];
        Source[] sources = [frontLeft, topSideLeft, topSideRight, objectSource];
        int[] channelIDs = new int[sources.Length];

        DolbyAtmosMasterFormatRootFile exported = new(staticObjects, sources, channelIDs);
        string exportedText = ExportToString(exported);
        DolbyAtmosMasterFormatRootFile parsed = new(exportedText, sources.Length);

        CollectionAssert.AreEqual(exported.Channels, parsed.Channels);
        CollectionAssert.AreEqual(exported.ObjectMapping, parsed.ObjectMapping);
        Assert.AreEqual(exported.BedChannelCount, parsed.BedChannelCount);
    }

    /// <summary>
    /// Tests that parsing normalizes imported object IDs to the canonical exported sequence.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_RoundTrip_NormalizesImportedObjectIds() {
        string yaml = BuildExportYaml(
            "scBedConfiguration: [100, 101]",
            [
                "          - channel: L",
                "            ID: 100",
                "          - channel: Lts",
                "            ID: 101"
            ],
            [
                "      - ID: 42",
                "      - ID: 64"
            ]);

        DolbyAtmosMasterFormatRootFile parsed = new(yaml, 4);

        Assert.AreEqual(2, parsed.BedChannelCount);
        CollectionAssert.AreEqual(new[] { ReferenceChannel.FrontLeft, ReferenceChannel.TopSideLeft }, parsed.Channels);
        CollectionAssert.AreEqual(new[] { 100, 101, 10, 11 }, parsed.ObjectMapping);
        AssertExport(parsed, BuildExportYaml(
            "scBedConfiguration: [100, 101]",
            [
                "          - channel: L",
                "            ID: 100",
                "          - channel: Lts",
                "            ID: 101"
            ],
            [
                "      - ID: 10",
                "      - ID: 11"
            ]));
    }

    /// <summary>
    /// Tests parsing a root file whose bed instance has an empty channel array.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_EmptyChannelsCollection_UsesObjectMapping() {
        string yaml = BuildExportYaml(
            "scBedConfiguration: []",
            [],
            [
                "      - ID: 10",
                "      - ID: 11"
            ]);

        DolbyAtmosMasterFormatRootFile root = new(yaml, 2);

        Assert.AreEqual(0, root.BedChannelCount);
        CollectionAssert.AreEqual(Array.Empty<ReferenceChannel>(), root.Channels);
        CollectionAssert.AreEqual(new[] { 10, 11 }, root.ObjectMapping);
    }

    /// <summary>
    /// Tests parsing a root file whose object list is empty.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_EmptyObjectsCollection_UsesBedChannels() {
        string yaml = BuildExportYaml(
            "scBedConfiguration: [0, 1]",
            [
                "          - channel: L",
                "            ID: 0",
                "          - channel: R",
                "            ID: 1"
            ],
            []);

        DolbyAtmosMasterFormatRootFile root = new(yaml, 2);

        Assert.AreEqual(2, root.BedChannelCount);
        CollectionAssert.AreEqual(new[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight }, root.Channels);
        CollectionAssert.AreEqual(new[] { 0, 1 }, root.ObjectMapping);
    }

    /// <summary>
    /// Tests that multiple presentations are rejected.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_RejectsMultiplePresentations() {
        string yaml = string.Join("\n", [
            "version: 0.5.1",
            "presentations:",
            "  - type: home",
            "    simplified: false",
            "    metadata: memory.atmos.metadata",
            "    audio: memory.atmos.audio",
            "    offset: 0.0",
            "    fps: 24",
            "    scBedConfiguration: []",
            "    creationTool: Cavern",
            "    creationToolVersion: " + Listener.Version,
            "    bedInstances:",
            "      - channels: []",
            "  - type: home",
            "    simplified: false",
            "    metadata: memory.atmos.metadata",
            "    audio: memory.atmos.audio",
            "    offset: 0.0",
            "    fps: 24",
            "    scBedConfiguration: []",
            "    creationTool: Cavern",
            "    creationToolVersion: " + Listener.Version,
            "    bedInstances:",
            "      - channels: []"
        ]);

        Assert.ThrowsException<CorruptionException>(() => new DolbyAtmosMasterFormatRootFile(yaml, 0));
    }

    /// <summary>
    /// Tests that multiple bed instances are rejected.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_RejectsMultipleBedInstances() {
        string yaml = string.Join("\n", [
            "version: 0.5.1",
            "presentations:",
            "  - type: home",
            "    simplified: false",
            "    metadata: memory.atmos.metadata",
            "    audio: memory.atmos.audio",
            "    offset: 0.0",
            "    fps: 24",
            "    scBedConfiguration: []",
            "    creationTool: Cavern",
            "    creationToolVersion: " + Listener.Version,
            "    bedInstances:",
            "      - channels: []",
            "      - channels: []"
        ]);

        Assert.ThrowsException<CorruptionException>(() => new DolbyAtmosMasterFormatRootFile(yaml, 0));
    }

    /// <summary>
    /// Tests that unknown channel names are rejected.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_RejectsUnknownChannelName() {
        string yaml = BuildExportYaml(
            "scBedConfiguration: [0]",
            [
                "          - channel: XYZ",
                "            ID: 0"
            ],
            []);

        Assert.ThrowsException<CorruptionException>(() => new DolbyAtmosMasterFormatRootFile(yaml, 1));
    }

    /// <summary>
    /// Tests that malformed channel entries are rejected.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_RejectsMalformedChannelEntry() {
        string yaml = BuildExportYaml(
            "scBedConfiguration: [0]",
            [
                "          - channel: L",
                "            ID: nope"
            ],
            []);

        Assert.ThrowsException<CorruptionException>(() => new DolbyAtmosMasterFormatRootFile(yaml, 1));
    }

    /// <summary>
    /// Tests that missing object lists are rejected when the PCM stream requires them.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_RejectsMissingObjectList() {
        string yaml = string.Join("\n", [
            "version: 0.5.1",
            "presentations:",
            "  - type: home",
            "    simplified: false",
            "    metadata: memory.atmos.metadata",
            "    audio: memory.atmos.audio",
            "    offset: 0.0",
            "    fps: 24",
            "    scBedConfiguration: [0]",
            "    creationTool: Cavern",
            "    creationToolVersion: " + Listener.Version,
            "    bedInstances:",
            "      - channels:",
            "          - channel: L",
            "            ID: 0"
        ]);

        Assert.ThrowsException<CorruptionException>(() => new DolbyAtmosMasterFormatRootFile(yaml, 2));
    }

    /// <summary>
    /// Tests that more objects than input streams are rejected.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_RejectsTooManyObjects() {
        string yaml = BuildExportYaml(
            "scBedConfiguration: [0]",
            [
                "          - channel: L",
                "            ID: 0"
            ],
            [
                "      - ID: 10"
            ]);

        Assert.ThrowsException<CorruptionException>(() => new DolbyAtmosMasterFormatRootFile(yaml, 1));
    }

    /// <summary>
    /// Tests that object counts must match the number of PCM object streams.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Parse_RejectsObjectCountMismatch() {
        string yaml = BuildExportYaml(
            "scBedConfiguration: [0]",
            [
                "          - channel: L",
                "            ID: 0"
            ],
            [
                "      - ID: 10"
            ]);

        Assert.ThrowsException<CorruptionException>(() => new DolbyAtmosMasterFormatRootFile(yaml, 3));
    }

    /// <summary>
    /// Creates an export root YAML text for assertions and parser tests.
    /// </summary>
    static string BuildExportYaml(string scBedConfiguration, string[] bedLines, string[] objectLines) {
        List<string> lines = [
            "version: 0.5.1",
            "presentations:",
            "  - type: home",
            "    simplified: false",
            "    metadata: memory.atmos.metadata",
            "    audio: memory.atmos.audio",
            "    offset: 0.0",
            "    fps: 24",
            "    " + scBedConfiguration,
            "    creationTool: Cavern",
            "    creationToolVersion: " + Listener.Version,
            "    bedInstances:"
        ];

        if (bedLines.Length == 0) {
            lines.Add("      - channels: []");
        } else {
            lines.Add("      - channels:");
            lines.AddRange(bedLines);
        }

        if (objectLines.Length == 0) {
            lines.Add("    objects: []");
        } else {
            lines.Add("    objects:");
            lines.AddRange(objectLines);
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Exports a root file to a memory buffer and returns the UTF-8 text.
    /// </summary>
    static string ExportToString(DolbyAtmosMasterFormatRootFile root) {
        using MemoryStream stream = new();
        root.Export(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Compares the exported YAML against the expected text after normalizing line endings.
    /// </summary>
    static void AssertExport(DolbyAtmosMasterFormatRootFile root, string expected) {
        Assert.AreEqual(Normalize(expected), Normalize(ExportToString(root)));
    }

    /// <summary>
    /// Normalizes line endings for deterministic string comparison.
    /// </summary>
    static string Normalize(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd('\n');
}
