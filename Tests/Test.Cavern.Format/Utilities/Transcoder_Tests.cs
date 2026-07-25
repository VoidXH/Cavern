using Cavern;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Environment;
using Cavern.Format.Utilities;

namespace Test.Cavern.Format.Utilities;

/// <summary>
/// Tests the <see cref="Transcoder"/> class.
/// </summary>
[TestClass]
public class Transcoder_Tests {
    /// <summary>
    /// Tests transcoding a mono stream using RIFFWave writer.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TranscodeMono_RIFFWave() {
        // Write input to a stream
        using MemoryStream inputStream = new();
        using RIFFWaveWriter writerIn = new(inputStream, 1, 4 * Consts.sampleRate, Consts.sampleRate, BitDepth.Float32);
        writerIn.WriteHeader();
        writerIn.WriteBlock(AudioSamples.Sweep4Sec, 0, AudioSamples.Sweep4Sec.LongLength);
        inputStream.Position = 0;

        // Transcode
        using MemoryStream outputStream = new();
        using AudioReader reader = AudioReader.Open(inputStream);
        reader.ReadHeader();  // Must read header first to populate ChannelCount, Length, SampleRate, Bits
        using AudioWriter writerOut = AudioWriter.Create(outputStream, Container.RIFFWave, reader.ChannelCount, reader.Length, reader.SampleRate, reader.Bits);
        Transcoder.Transcode(reader, writerOut);

        // Verify - read back from output stream without closing it
        outputStream.Position = 0;
        using RIFFWaveReader readerOut = new(outputStream);
        readerOut.ReadHeader();
        float[] result = new float[readerOut.Length * readerOut.ChannelCount];
        readerOut.ReadBlock(result, 0, result.Length);
        AudioReaderWriter_Tests.CompareWaveforms(AudioSamples.Sweep4Sec, result, Consts.epsilon);
    }

    /// <summary>
    /// Tests transcoding a stereo stream using RIFFWave writer.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TranscodeStereo_RIFFWave() {
        // Write input to a stream
        using MemoryStream inputStream = new();
        using RIFFWaveWriter writerIn = new(inputStream, 2, 4 * Consts.sampleRate, Consts.sampleRate, BitDepth.Float32);
        writerIn.WriteHeader();
        writerIn.WriteBlock(AudioSamples.Sweep4SecStereo, 0, AudioSamples.Sweep4SecStereo[0].LongLength);
        inputStream.Position = 0;

        // Transcode
        using MemoryStream outputStream = new();
        using AudioReader reader = AudioReader.Open(inputStream);
        reader.ReadHeader();  // Must read header first
        using AudioWriter writerOut = AudioWriter.Create(outputStream, Container.RIFFWave, reader.ChannelCount, reader.Length, reader.SampleRate, reader.Bits);
        Transcoder.Transcode(reader, writerOut);

        // Verify - read back from output stream without closing it
        outputStream.Position = 0;
        using RIFFWaveReader readerOut = new(outputStream);
        readerOut.ReadHeader();
        float[][] result = new float[readerOut.ChannelCount][];
        for (int c = 0; c < readerOut.ChannelCount; c++) {
            result[c] = new float[readerOut.Length];
        }
        readerOut.ReadBlock(result, 0, readerOut.Length);
        CollectionAssert.AreEqual(AudioSamples.Sweep4SecStereo[0], result[0]);
        CollectionAssert.AreEqual(AudioSamples.Sweep4SecStereo[1], result[1]);
    }

    /// <summary>
    /// Tests transcoding a source via <see cref="Transcoder.Transcode(EnvironmentWriter)"/> with actual audio data.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TranscodeEnvironment_MonoSweep() {
        Listener listener = new(false) {
            SampleRate = Consts.sampleRate,
            UpdateRate = 240
        };

        Clip clip = new(AudioSamples.Sweep4Sec, 1, Consts.sampleRate);
        Source source = new() {
            Clip = clip,
            Loop = true
        };
        source.Play();
        listener.AttachSource(source);

        long length = 4 * Consts.sampleRate; // 4 seconds
        using MemoryStream stream = new();
        using LimitlessAudioFormatEnvironmentWriter writer = new(stream, listener, length, BitDepth.Float32);
        Transcoder.Transcode(writer);

        // Read back and verify
        stream.Position = 0;
        using LimitlessAudioFormatReader reader = new(stream);
        reader.ReadHeader();
        Assert.AreEqual(length, reader.Length);
        Assert.AreEqual(1, reader.ChannelCount); // 1 source -> 1 audio channel in LAF

        float[] result = new float[reader.Length];
        reader.ReadBlock(result, 0, result.Length);
        AudioReaderWriter_Tests.CompareWaveforms(AudioSamples.Sweep4Sec, result, Consts.epsilon);
    }

    /// <summary>
    /// Tests transcoding a reader to an <see cref="EnvironmentWriter"/> via <see cref="Transcoder.Transcode(AudioReader, EnvironmentWriter)"/>.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TranscodeReaderToEnvironment_RIFFWave() {
        // Write input to a stream
        using MemoryStream inputStream = new();
        using RIFFWaveWriter writerIn = new(inputStream, 1, 4 * Consts.sampleRate, Consts.sampleRate, BitDepth.Float32);
        writerIn.WriteHeader();
        writerIn.WriteBlock(AudioSamples.Sweep4Sec, 0, AudioSamples.Sweep4Sec.LongLength);
        inputStream.Position = 0;

        // Read back, create environment, transcode
        using MemoryStream outputStream = new();
        using AudioReader reader = AudioReader.Open(inputStream);
        reader.ReadHeader();

        Listener listener = new(false);
        long length = reader.Length;
        using LimitlessAudioFormatEnvironmentWriter writer = new(outputStream, listener, length, BitDepth.Float32);
        Transcoder.Transcode(reader, writer);

        // Read back and verify
        outputStream.Position = 0;
        using LimitlessAudioFormatReader readerOut = new(outputStream);
        readerOut.ReadHeader();
        Assert.AreEqual(length, readerOut.Length);
        Assert.AreEqual(1, readerOut.ChannelCount);

        float[] result = new float[readerOut.Length];
        readerOut.ReadBlock(result, 0, result.Length);
        AudioReaderWriter_Tests.CompareWaveforms(AudioSamples.Sweep4Sec, result, Consts.epsilon);
    }
}
