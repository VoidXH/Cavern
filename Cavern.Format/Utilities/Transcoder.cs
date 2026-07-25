using System;

using Cavern.Channels;
using Cavern.Format.Environment;
using Cavern.Format.Exceptions;
using Cavern.Format.Renderers;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Utility class for transcoding audio from an <see cref="AudioReader"/> to an <see cref="AudioWriter"/>,
    /// or rendering a <see cref="Listener"/> environment to an <see cref="EnvironmentWriter"/>.
    /// </summary>
    /// <remarks>These methods do not dispose the reader or the writer. The caller is responsible for disposing the writer after transcoding completes.</remarks>
    public static class Transcoder {
        /// <summary>
        /// Transcodes audio from an <see cref="AudioReader"/> to an <see cref="AudioWriter"/>. Reads the entire audio content from the reader and writes it to the writer.
        /// </summary>
        /// <param name="reader">The audio reader to read from</param>
        /// <param name="writer">The audio writer to write to</param>
        public static void Transcode(AudioReader reader, AudioWriter writer) => Transcode(reader, writer, Listener.DefaultSampleRate);

        /// <summary>
        /// Transcodes audio from an <see cref="AudioReader"/> to an <see cref="AudioWriter"/>. Reads the entire audio content from the reader and writes it to the writer.
        /// </summary>
        /// <param name="reader">The audio reader to read from</param>
        /// <param name="writer">The audio writer to write to</param>
        /// <param name="blockSize">Number of samples per channel to process per block</param>
        public static void Transcode(AudioReader reader, AudioWriter writer, int blockSize) {
            if (reader == null) {
                throw new ArgumentNullException(nameof(reader));
            }
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }

            if (reader.ChannelCount != writer.ChannelCount) {
                throw new ChannelCountMismatchException();
            }
            if (reader.SampleRate != writer.SampleRate) {
                throw new SampleRateMismatchException();
            }
            if (reader.Bits != writer.Bits) {
                throw new InvalidBitDepthException(reader.Bits);
            }

            reader.ReadHeader();
            writer.WriteHeader();

            long totalSamples = reader.Length * reader.ChannelCount;
            long samplesProcessed = 0;
            float[] buffer = new float[blockSize * reader.ChannelCount];

            while (samplesProcessed < totalSamples) {
                long samplesToRead = Math.Min(buffer.LongLength, totalSamples - samplesProcessed);
                reader.ReadBlock(buffer, 0, samplesToRead);
                writer.WriteBlock(buffer, 0, samplesToRead);
                samplesProcessed += samplesToRead;
            }
        }

        /// <summary>
        /// Renders a <see cref="Listener"/> environment to an <see cref="EnvironmentWriter"/>.
        /// Writes all frames from the environment by calling <see cref="EnvironmentWriter.WriteNextFrame"/> until the writer's <see cref="EnvironmentWriter.Length"/> is reached.
        /// </summary>
        /// <param name="environment">The environment writer to write frames to</param>
        public static void Transcode(EnvironmentWriter environment) {
            if (environment == null) {
                throw new ArgumentNullException(nameof(environment));
            }

            for (long sample = 0; sample < environment.Length; sample += environment.Source.UpdateRate) {
                environment.WriteNextFrame();
            }
        }

        /// <summary>
        /// Renders an <see cref="AudioReader"/>'s content to an <see cref="EnvironmentWriter"/>. Gets the reader's <see cref="Renderer"/>, attaches its sources to the
        /// environment's <see cref="Listener"/>, and exports all frames. This is the object-based equivalent of <see cref="Transcode(AudioReader, AudioWriter)"/>.
        /// </summary>
        /// <param name="reader">The audio reader providing the content to export</param>
        /// <param name="environment">The environment writer to write frames to</param>
        public static void Transcode(AudioReader reader, EnvironmentWriter environment) {
            if (reader == null) {
                throw new ArgumentNullException(nameof(reader));
            }
            if (environment == null) {
                throw new ArgumentNullException(nameof(environment));
            }

            reader.Reset();
            environment.Source.SampleRate = reader.SampleRate;

            Renderer renderer = reader.GetRenderer();
            environment.Source.AttachSources(renderer.Objects);

            for (long sample = 0; sample < environment.Length; sample += environment.Source.UpdateRate) {
                environment.WriteNextFrame();
            }
        }
    }
}
