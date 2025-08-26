using System.IO;

using Cavern.Format.Consts;
using Cavern.Format.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Minimal Core Audio Format file writer.
    /// </summary>
    public class CoreAudioFormatWriter : UncompressedWriter {
        /// <summary>
        /// Minimal Core Audio Format file writer.
        /// </summary>
        public CoreAudioFormatWriter(Stream writer, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(writer, channelCount, length, sampleRate, bits) { }

        /// <summary>
        /// Minimal Core Audio Format file writer.
        /// </summary>
        public CoreAudioFormatWriter(string path, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(path, channelCount, length, sampleRate, bits) { }

        /// <inheritdoc/>
        public override void WriteHeader() {
            writer.WriteAny(CoreAudioFormatConsts.syncWord);
            writer.WriteAnyBE((ushort)1); // Version
            writer.WriteAny((ushort)0); // Flags

            writer.WriteAny(CoreAudioFormatConsts.audioDescriptionChunk);
            writer.WriteAnyBE((ulong)32); // Chunk size
            writer.WriteAnyBE((double)SampleRate);
            writer.WriteAny("lpcm");
            writer.WriteAnyBE(2); // Format flags - 2 is little-endian integer
            uint frameSize = (uint)(((long)Bits >> 3) * ChannelCount);
            writer.WriteAnyBE(frameSize);
            writer.WriteAnyBE(1); // Frames per packet
            writer.WriteAnyBE(ChannelCount);
            writer.WriteAnyBE((uint)Bits);

            writer.WriteAny(RIFFWaveConsts.dataSync);
            writer.WriteAnyBE(frameSize * Length);
            writer.WriteAny(0); // Edit count
        }
    }
}
