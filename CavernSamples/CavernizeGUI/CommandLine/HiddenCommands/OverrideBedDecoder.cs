using Cavern.Format;
using Cavern.Format.Decoders;
using Cavern.Format.Utilities;

namespace CavernizeGUI.CommandLine.HiddenCommands {
    /// <summary>
    /// Converts an Enhanced AC-3 bitstream to raw samples, replaces the PCM data with external.
    /// </summary>
    public class OverrideBedDecoder : EnhancedAC3Decoder {
        /// <summary>
        /// Stream to override the PCM data with. Only applies to the source PCM data, not the JOC-decoded objects.
        /// </summary>
        readonly AudioReader overrider;

        /// <summary>
        /// Converts an Enhanced AC-3 bitstream to raw samples, replaces the PCM data with external.
        /// </summary>
        public OverrideBedDecoder(BlockBuffer<byte> reader, long fileSize, AudioReader overrider) : base(reader, fileSize) =>
            this.overrider = overrider;

        /// <summary>
        /// Decode a new frame if the cached samples are already fetched.
        /// </summary>
        protected override float[] DecodeFrame() {
            float[] result = base.DecodeFrame();
            overrider?.ReadBlock(result, 0, result.Length);
            return result;
        }
    }
}