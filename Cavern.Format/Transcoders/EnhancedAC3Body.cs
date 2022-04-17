using Cavern.Format.Utilities;
using Cavern.Remapping;
using System.Collections.Generic;

namespace Cavern.Format.Transcoders {
    /// <summary>
    /// Renders a single E-AC-3 substream and holds inter-frame data.
    /// </summary>
    partial class EnhancedAC3Body {
        /// <summary>
        /// Used full bandwidth channels.
        /// </summary>
        public IReadOnlyList<ReferenceChannel> Channels => channels;

        /// <summary>
        /// Used full bandwidth channels.
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

        public EnhancedAC3Body(EnhancedAC3Header header) => this.header = header;

        public void PrepareUpdate(BitExtractor extractor) {
            this.extractor = extractor;
            channels = header.GetChannelArrangement();
            if (FrameResult == null || FrameResult.Length != channels.Length) {
                FrameResult = new float[channels.Length][];
                for (int i = 0; i < channels.Length; ++i)
                    FrameResult[i] = new float[header.Blocks * 256];
            }
            if (LFEResult == null && header.LFE)
                LFEResult = new float[header.Blocks * 256];

            CreateCacheTables(header.Blocks, channels.Length);
            if (header.Decoder == EnhancedAC3.Decoders.EAC3)
                AudioFrame();
            else {
                blkswe = true;
                dithflage = true;
                bamode = true;
                snroffststr = -1;
                firstcplleak = false;
                // TODO: disable AHT when it's implemented
            }
        }

        public void Update() {
            // Create or reuse per-channel outputs
            for (int block = 0; block < header.Blocks; ++block)
                AudioBlock(block);
        }
    }
}