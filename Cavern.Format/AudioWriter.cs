using System.IO;

namespace Cavern.Format {
    /// <summary>Abstract audio file writer.</summary>
    public abstract class AudioWriter {
        /// <summary>File writer object.</summary>
        protected BinaryWriter Writer;
        /// <summary>Output channel count.</summary>
        protected int ChannelCount;
        /// <summary>Output length in samples.</summary>
        protected long Length;
        /// <summary>Output sample rate.</summary>
        protected int SampleRate;
        /// <summary>Output bit depth.</summary>
        protected BitDepth Bits;

        /// <summary>Abstract audio file writer.</summary>
        /// <param name="Writer">File writer object</param>
        /// <param name="ChannelCount">Output channel count</param>
        /// <param name="Length">Output length in samples</param>
        /// <param name="SampleRate">Output sample rate</param>
        /// <param name="Bits">Output bit depth</param>
        public AudioWriter(BinaryWriter Writer, int ChannelCount, long Length, int SampleRate, BitDepth Bits) {
            this.Writer = Writer;
            this.ChannelCount = ChannelCount;
            this.Length = Length;
            this.SampleRate = SampleRate;
            this.Bits = Bits;
        }

        /// <summary>Create the file header.</summary>
        public abstract void WriteHeader();

        /// <summary>Write a block of samples.</summary>
        /// <param name="Samples">Samples to write</param>
        /// <param name="From">Start position in the input array (inclusive)</param>
        /// <param name="To">End position in the input array (exclusive)</param>
        public abstract void WriteBlock(float[] Samples, long From, long To);

        /// <summary>Write the entire file.</summary>
        /// <param name="Samples">All input samples</param>
        public void Write(float[] Samples) {
            Length = Samples.LongLength;
            WriteHeader();
            WriteBlock(Samples, 0, Length);
            Writer.Close();
        }
    }
}