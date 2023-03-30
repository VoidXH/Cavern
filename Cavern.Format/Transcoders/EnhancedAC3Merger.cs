using System;
using System.IO;

using Cavern.Channels;
using Cavern.Format.Consts;
using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    /// <summary>
    /// Combines multiple E-AC-3 streams, with a custom channel mapping. E-AC-3 can contain channel-based 3D audio up to 9.1.6,
    /// and combining a 5.1 stream with quad or stereo additions can reach it.
    /// </summary>
    /// <remarks>The first file to merge must be a valid E-AC-3 file on its own, not a dependent substream.
    /// This is required by standard.</remarks>
    public class EnhancedAC3Merger {
        /// <summary>
        /// A single stream to merge. Easy access to all required objects for reading an E-AC-3 header.
        /// </summary>
        class Source {
            /// <summary>
            /// Alignment and content information of the stream.
            /// </summary>
            public EnhancedAC3Header Header { get; }

            /// <summary>
            /// Actual coded data of the stream.
            /// </summary>
            public EnhancedAC3Body Body { get; }

            /// <summary>
            /// File accessor to the <see cref="Header"/>.
            /// </summary>
            public BlockBuffer<byte> Reader { get; }

            /// <summary>
            /// Raw bytes of the last decoded frame.
            /// </summary>
            public BitExtractor Frame { get; private set; }

            /// <summary>
            /// Number of discrete channels in this input stream.
            /// </summary>
            public int ChannelCount { get; }

            /// <summary>
            /// Create all required objects for an E-AC-3 stream.
            /// </summary>
            public Source(Stream source) {
                Header = new EnhancedAC3Header();
                Body = new EnhancedAC3Body(Header);
                Reader = BlockBuffer<byte>.Create(source, FormatConsts.blockSize);
                Frame = Header.Decode(Reader);
                ChannelCount = Header.GetChannelArrangement().Length;
                if (Header.LFE) {
                    ++ChannelCount;
                }
            }

            /// <summary>
            /// Read the next frame of the input stream and return if it's available.
            /// </summary>
            /// <remarks>After calling this function, the next read bit in <see cref="Frame"/>
            /// will be the start of the audio data.</remarks>
            public bool NextFrame() => (Frame = Header.Decode(Reader)) != null;
        }

        /// <summary>
        /// Streams to merge.
        /// </summary>
        readonly Source[] sources;

        /// <summary>
        /// Target channel order.
        /// </summary>
        readonly ReferenceChannel[] layout;

        /// <summary>
        /// File write stream.
        /// </summary>
        readonly FileStream output;

        /// <summary>
        /// Construct an E-AC-3 merger for a target layout.
        /// </summary>
        public EnhancedAC3Merger(Stream[] sources, ReferenceChannel[] layout, string path) {
            this.sources = new Source[sources.Length];
            this.layout = layout;
            int totalChannels = 0;
            for (int i = 0; i < sources.Length; i++) {
                this.sources[i] = new Source(sources[i]);
                totalChannels += this.sources[i].ChannelCount;
            }
            if (totalChannels != layout.Length) {
                throw new ArgumentOutOfRangeException(nameof(layout));
            }

            SetupHeaders();
            output = File.Open(path, FileMode.Create);
        }

        /// <summary>
        /// Processes the next sync frame (1536 samples), returns if the transcoding is done.
        /// </summary>
        public bool ProcessFrame() {
            for (int i = 0; i < sources.Length; i++) {
                Source source = sources[i];
                BitPlanter encoder = source.Header.Encode();
                for (int block = 0; block < source.Header.Blocks; block++) {
                    if (block == 0) {
                        source.Body.PrepareUpdate(source.Frame);
                    }
                    EnhancedAC3Body stateBefore = source.Body.CreateEncodeBackup();
                    source.Body.DecodeAudioBlock(block);
                    EnhancedAC3Body stateAfter = source.Body.CreateEncodeBackup();

                    source.Body.RestoreEncodeBackup(stateBefore);
                    if (block == 0) {
                        source.Body.PrepareUpdate(encoder);
                    }
                    source.Body.EncodeAudioBlock(encoder, block);
                    source.Body.RestoreEncodeBackup(stateAfter);
                }
                encoder.Write(0, source.Header.WordsPerSyncframe * 16 - encoder.BitsWritten - 16 /* CRC */); // Padding
                encoder.Write(encoder.CalculateCRC16(16, 0x8005), 16);
                encoder.WriteToStream(output);
            }

            // Prepare the next frame
            for (int i = 0; i < sources.Length; i++) {
                if (!sources[i].NextFrame()) {
                    output.Close();
                    return true;
                }
            }
            SetupHeaders();
            return false;
        }

        /// <summary>
        /// Modify the inputs' headers to have a forced channel mapping.
        /// </summary>
        void SetupHeaders() {
            int channelsUsed = sources[0].ChannelCount;
            for (int i = 1; i < sources.Length; i++) {
                sources[i].Header.SetChannelArrangement(layout[channelsUsed..(channelsUsed += sources[i].ChannelCount)]);
                sources[i].Header.SubstreamID = 7 + i;
                sources[i].Header.StreamTypeOut = EnhancedAC3.StreamTypes.Dependent;
            }
        }
    }
}