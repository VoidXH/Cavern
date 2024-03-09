using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Cavern.Channels;
using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    /// <summary>
    /// Renders a single E-AC-3 substream and holds inter-frame data.
    /// </summary>
    partial class EnhancedAC3Body {
        /// <summary>
        /// Used full bandwidth channels. Maps the channel indexes to actual contained channels.
        /// </summary>
        public IReadOnlyList<ReferenceChannel> Channels => channels;

        /// <summary>
        /// Used full bandwidth channels. Maps the channel indexes to actual contained channels.
        /// </summary>
        ReferenceChannel[] channels;

        /// <summary>
        /// Full bandwidth samples from the last decoded frame.
        /// </summary>
        public float[][] FrameResult { get; private set; }

        /// <summary>
        /// LFE samples from the last decoded frame.
        /// </summary>
        public float[] LFEResult { get; private set; }

        /// <summary>
        /// Source of decodable data.
        /// </summary>
        BitExtractor extractor;

        /// <summary>
        /// Header data container and reader.
        /// </summary>
        /// <remarks>Reading is done in the decoder.</remarks>
        readonly EnhancedAC3Header header;

        /// <summary>
        /// Renders a single E-AC-3 substream and holds inter-frame data.
        /// </summary>
        public EnhancedAC3Body(EnhancedAC3Header header) => this.header = header;

        /// <summary>
        /// Do the mandatory steps before reading the audio blocks.
        /// </summary>
        public void PrepareUpdate(BitExtractor extractor) {
            this.extractor = extractor;
            channels = header.GetChannelArrangement();
            if (FrameResult == null || FrameResult.Length != channels.Length) {
                FrameResult = new float[channels.Length][];
                for (int i = 0; i < channels.Length; ++i) {
                    FrameResult[i] = new float[header.Blocks * 256];
                }
            }
            if (LFEResult == null && header.LFE) {
                LFEResult = new float[header.Blocks * 256];
            }

            if (chexpstr == null) { // If caches don't exist, create them
                CreateCacheTables(header.Blocks, channels.Length);
            }
            if (header.Decoder == EnhancedAC3.Decoders.EAC3) {
                DecodeAudioFrame();
            } else {
                blkswe = true;
                dithflage = true;
                bamode = true;
                snroffststr = -1;
                frmfgaincode = false;
                firstcplleak = false;
                dbaflde = true;
                skipFieldSyntaxEnabled = true;
                // TODO: disable AHT when it's implemented
            }
        }

        /// <summary>
        /// Do the mandatory steps before writing the audio blocks.
        /// </summary>
        public void PrepareUpdate(BitPlanter planter) {
            if (header.Decoder == EnhancedAC3.Decoders.EAC3) {
                EncodeAudioFrame(planter);
            }
        }

        /// <summary>
        /// Combine the found auxillary data.
        /// </summary>
        public BitExtractor GetAuxData() => new BitExtractor(auxData, auxDataPos);

        /// <summary>
        /// Create or reuse per-channel outputs and separate auxillary bitstream.
        /// </summary>
        public void Update() {
            auxDataPos = 0;
            for (int block = 0; block < header.Blocks; ++block) {
                DecodeAudioBlock(block);
            }
            ReadAux();
        }

        /// <summary>
        /// Read the auxillary data field and add it to <see cref="auxData"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ReadAux() {
            extractor.Position = extractor.BackPosition - 32;
            int auxLength = extractor.Read(14);
            if (extractor.ReadBit()) { // Auxillary data present
                extractor.Position = extractor.BackPosition - 32 - auxLength;
                extractor.ReadBytesInto(ref auxData, ref auxDataPos, auxLength);
            }
        }
    }
}