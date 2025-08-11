using Cavern.Format.Decoders;

namespace Cavern.Format.Consts {
    /// <summary>
    /// Used for both <see cref="MeridianLosslessPackingDecoder"/>.
    /// </summary>
    internal static class MeridianLosslessPackingConsts {
        /// <summary>
        /// Magic number marking a major sync block in an MLP stream.
        /// </summary>
        public const int syncWord = unchecked((int)0xF8726FBA);

        /// <summary>
        /// Bytes that must be read before determining the frame size.
        /// </summary>
        public const int mustDecode = 8;

        /// <summary>
        /// Additional integrity check for major sync blocks.
        /// </summary>
        public const ushort majorSyncSignature = 0xB752;
    }
}
