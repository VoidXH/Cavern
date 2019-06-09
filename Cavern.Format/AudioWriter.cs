using System.IO;

namespace Cavern.Format {
    /// <summary>Abstract audio file writer.</summary>
    public abstract class AudioWriter {
        /// <summary>File writer object.</summary>
        protected BinaryWriter writer;
        /// <summary>Output channel count.</summary>
        protected int channelCount;
        /// <summary>Output length in samples.</summary>
        protected long length;
        /// <summary>Output sample rate.</summary>
        protected int sampleRate;
        /// <summary>Output bit depth.</summary>
        protected BitDepth bits;

        /// <summary>Abstract audio file writer.</summary>
        /// <param name="writer">File writer object</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public AudioWriter(BinaryWriter writer, int channelCount, long length, int sampleRate, BitDepth bits) {
            this.writer = writer;
            this.channelCount = channelCount;
            this.length = length;
            this.sampleRate = sampleRate;
            this.bits = bits;
        }

        /// <summary>Create the file header.</summary>
        public abstract void WriteHeader();

        /// <summary>Write a block of samples.</summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public abstract void WriteBlock(float[] samples, long from, long to);

        /// <summary>Write the entire file.</summary>
        /// <param name="samples">All input samples</param>
        public void Write(float[] samples) {
            length = samples.LongLength;
            WriteHeader();
            WriteBlock(samples, 0, length);
            writer.Close();
        }
    }
}