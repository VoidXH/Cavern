using System.Numerics;

using Cavern;
using Cavern.Channels;
using Cavern.Format;
using Cavern.Format.Decoders;
using Cavern.Format.Environment;
using Cavern.Format.Environment.Utilities;
using Cavern.Format.Renderers;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;
using Cavern.Format.Utilities;

namespace Test.Cavern.Format.Output;

/// <summary>
/// Tests for <see cref="BroadcastWaveFormatWriter"/> (ADM BWF) and <see cref="DolbyAtmosBWFWriter"/> export via <see cref="Transcoder"/>.
/// </summary>
[TestClass]
public class ADM_BWF_Tests {
    /// <summary>
    /// Tests transcoding a 5.1 bed to <see cref="BroadcastWaveFormatWriter"/> and verifying ADM <c>DirectSpeakers</c> assignments.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TranscodeBWF_51Channels() {
        Listener listener = Create51Listener();

        byte[] writtenData;
        using (MemoryStream stream = new()) {
            long length = Consts.sampleRate; // 1 second
            using (BroadcastWaveFormatWriter writer = new(stream, listener, length, BitDepth.Float32)) {
                Transcoder.Transcode(writer);
            }
            writtenData = stream.ToArray();
        }

        using MemoryStream readStream = new(writtenData);
        using RIFFWaveReader reader = new(readStream);
        reader.ReadHeader();
        Assert.AreEqual(6, reader.ChannelCount);

        readStream.Position = 0;
        RIFFWaveDecoder decoder = new(readStream);
        Assert.IsNotNull(decoder.ADM);

        for (int i = 0; i < 6; i++) {
            ADMChannelFormat movement = decoder.ADM.Movements[i];
            Assert.AreEqual(ADMPackType.DirectSpeakers, movement.Type, $"Channel {i} should be DirectSpeakers (static bed)");
            Assert.AreEqual(1, movement.Blocks.Count, $"Channel {i} should have exactly 1 static block");

            Vector3 expectedPos = ChannelPrototype.AlternativePositions[(int)ChannelPrototype.ref510[i]];
            Vector3 actualPos = movement.Blocks[0].Position;
            Assert.IsTrue(ArePositionsClose(expectedPos, actualPos), $"Channel {ChannelPrototype.ref510[i]} position {actualPos} doesn't match expected {expectedPos}");
        }
    }

    /// <summary>
    /// Tests transcoding a 5.1 bed to <see cref="DolbyAtmosBWFWriter"/> and verifying ADM bed channels correctly embed the 6 real sources at
    /// their proper 5.1 positions in the 10-channel bed.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TranscodeAtmos_51Channels() {
        Listener listener = new(false) {
            SampleRate = Consts.sampleRate,
            UpdateRate = 240
        };

        float[] monoData = AudioSamples.Sweep4Sec;
        StaticSource[] staticSources = new StaticSource[6];
        for (int c = 0; c < 6; c++) {
            Clip clip = new(monoData, 1, Consts.sampleRate);
            Source source = new() {
                Clip = clip,
                Loop = true
            };
            source.Play();
            source.Position = ChannelPrototype.AlternativePositions[(int)ChannelPrototype.ref510[c]];
            listener.AttachSource(source);
            staticSources[c] = new StaticSource(ChannelPrototype.ref510[c], source);
        }

        byte[] writtenData;
        using (MemoryStream stream = new()) {
            long length = Consts.sampleRate; // 1 second
            using (DolbyAtmosBWFWriter writer = new(stream, listener, length, BitDepth.Float32, staticSources)) {
                Transcoder.Transcode(writer);
            }
            writtenData = stream.ToArray();
        }

        // Read back
        using MemoryStream readStream = new(writtenData);
        using RIFFWaveReader reader = new(readStream);
        reader.ReadHeader();
        Assert.AreEqual(10, reader.ChannelCount);

        readStream.Position = 0;
        RIFFWaveDecoder decoder = new(readStream);
        Assert.IsNotNull(decoder.ADM);
        Assert.IsTrue(decoder.ADM.Movements.Count >= 10);

        int bedCount = 0;
        int objectCount = 0;
        for (int i = 0; i < decoder.ADM.Movements.Count; i++) {
            ADMChannelFormat movement = decoder.ADM.Movements[i];
            if (movement.Type == ADMPackType.DirectSpeakers) {
                bedCount++;
            } else if (movement.Type == ADMPackType.Objects) {
                objectCount++;
            }
        }

        Assert.AreEqual(10, bedCount, "DolbyAtmosBWFWriter should have exactly 10 DirectSpeakers bed tracks");
        Assert.AreEqual(0, objectCount, "Static 5.1 content should not create dynamic object tracks");

        RIFFWaveRenderer renderer = (RIFFWaveRenderer)reader.GetRenderer();
        ReferenceChannel[] channels = renderer.GetChannels();
        int found = 0;
        foreach (ReferenceChannel expected in ChannelPrototype.ref510) {
            if (Array.IndexOf(channels, expected) >= 0) {
                found++;
            }
        }
        Assert.AreEqual(6, found, "All 6 5.1 channels should be identifiable in the Atmos bed");
    }

    /// <summary>
    /// Creates a <see cref="Listener"/> with 5.1 channels and sources positioned accordingly.
    /// </summary>
    static Listener Create51Listener() {
        Listener listener = new(false) {
            SampleRate = Consts.sampleRate,
            UpdateRate = 240
        };

        float[] monoData = AudioSamples.Sweep4Sec;
        for (int c = 0; c < 6; c++) {
            Clip clip = new(monoData, 1, Consts.sampleRate);
            Source source = new() {
                Clip = clip,
                Loop = true
            };
            source.Play();
            source.Position = ChannelPrototype.AlternativePositions[(int)ChannelPrototype.ref510[c]] * Listener.EnvironmentSize;
            listener.AttachSource(source);
        }

        return listener;
    }

    /// <summary>
    /// Checks if two position vectors are within tolerance.
    /// </summary>
    static bool ArePositionsClose(Vector3 a, Vector3 b) => Math.Abs(a.X - b.X) < Consts.epsilon && Math.Abs(a.Y - b.Y) < Consts.epsilon && Math.Abs(a.Z - b.Z) < Consts.epsilon;
}
