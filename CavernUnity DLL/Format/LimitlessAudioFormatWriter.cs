using System;
using System.IO;

namespace Cavern.Format {
    /// <summary>Minimal Limitless Audio Format file writer.</summary>
    public class LimitlessAudioFormatWriter : AudioWriter {
        /// <summary>Limitless Audio Format indicator starting bytes.</summary>
        static readonly byte[] Limitless = new byte[9] { (byte)'L', (byte)'I', (byte)'M', (byte)'I', (byte)'T', (byte)'L', (byte)'E', (byte)'S', (byte)'S' };
        /// <summary>Header marker bytes.</summary>
        static readonly byte[] Head = new byte[4] { (byte)'H', (byte)'E', (byte)'A', (byte)'D', };

        /// <summary>Output channel information.</summary>
        Channel[] Channels;
        /// <summary>The past second for each channel.</summary>
        float[] Cache;
        /// <summary>Size of the <see cref="Cache"/>.</summary>
        int CacheLimit;
        /// <summary>Write position in the <see cref="Cache"/>. Used to check if the cache is full for block dumping.</summary>
        int CachePosition = 0;
        /// <summary>Total samples written in the file so far. Used to check the end of file and dump the unfilled last block.</summary>
        long TotalWritten = 0;

        /// <summary>Minimal Limitless Audio Format file writer.</summary>
        /// <param name="Writer">File writer object</param>
        /// <param name="ChannelCount">Output channel count</param>
        /// <param name="Length">Output length in samples</param>
        /// <param name="SampleRate">Output sample rate</param>
        /// <param name="Bits">Output bit depth</param>
        /// <param name="Channels">Output channel information</param>
        public LimitlessAudioFormatWriter(BinaryWriter Writer, int ChannelCount, long Length, int SampleRate, BitDepth Bits, Channel[] Channels) :
            base(Writer, ChannelCount, Length, SampleRate, Bits) {
            this.Channels = Channels;
            Cache = new float[CacheLimit = ChannelCount * SampleRate];
        }

        /// <summary>Create the file header.</summary>
        public override void WriteHeader() {
            Writer.Write(Limitless); // Limitless marker
            // No custom headers
            Writer.Write(Head); // Main header marker
            Writer.Write(new byte[] { Bits == BitDepth.Int8 ? (byte)0 : (Bits == BitDepth.Int16 ? (byte)1 : (byte)2), (byte)0 }); // Quality and channel mode indicator
            Writer.Write(BitConverter.GetBytes(ChannelCount)); // Channel/object count
            for (int i = 0; i < ChannelCount; ++i) { // Channel/object info
                Writer.Write(BitConverter.GetBytes(Channels[i].x)); // Rotation on X axis
                Writer.Write(BitConverter.GetBytes(Channels[i].y)); // Rotation on Y axis
                Writer.Write(Channels[i].LFE ? (byte)1 : (byte)0); // Low frequency
            }
            Writer.Write(BitConverter.GetBytes(SampleRate));
            Writer.Write(BitConverter.GetBytes(Length));
        }

        /// <summary>Output only the used channels from the last second.</summary>
        /// <param name="Until">Samples to dump from the <see cref="Cache"/></param>
        void DumpBlock(long Until) {
            bool[] ToWrite = new bool[ChannelCount];
            for (int Channel = 0; Channel < ChannelCount; ++Channel)
                for (int Sample = Channel; !ToWrite[Channel] && Sample < SampleRate; Sample += ChannelCount)
                    if (Cache[Sample] != 0)
                        ToWrite[Channel] = true;
            byte[] LayoutBytes = new byte[ChannelCount % 8 == 0 ? ChannelCount / 8 : (ChannelCount / 8 + 1)];
            for (int Channel = 0; Channel < ChannelCount; ++Channel) {
                if (ToWrite[Channel])
                    LayoutBytes[Channel / 8] += (byte)(1 << (Channel % 8));
            }
            Writer.Write(LayoutBytes);
            switch (Bits) {
                case BitDepth.Int8:
                    for (int i = 0; i < Until; ++i)
                        if (ToWrite[i % ChannelCount])
                            Writer.Write((byte)((Cache[i] + 1f) * 127f));
                    break;
                case BitDepth.Int16:
                    for (int i = 0; i < Until; ++i)
                        if (ToWrite[i % ChannelCount])
                            Writer.Write(BitConverter.GetBytes((short)(Cache[i] * 32767f)));
                    break;
                case BitDepth.Float32:
                    for (int i = 0; i < Until; ++i)
                        if (ToWrite[i % ChannelCount])
                            Writer.Write(BitConverter.GetBytes(Cache[i]));
                    break;
            }
            CachePosition = 0;
        }

        /// <summary>Write a block of samples.</summary>
        /// <param name="Samples">Samples to write</param>
        /// <param name="From">Start position in the input array (inclusive)</param>
        /// <param name="To">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[] Samples, long From, long To) {
            long DumpLength = To - From;
            while (From < To) {
                for (; From < To && CachePosition < CacheLimit; ++From)
                    Cache[CachePosition++] = Samples[From];
                if (CachePosition == CacheLimit)
                    DumpBlock(CacheLimit);
            }
            if ((TotalWritten += DumpLength) == Length)
                DumpBlock(CachePosition);
        }
    }
}