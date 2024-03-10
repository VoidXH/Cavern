using Cavern.Format.Container.MXF;

namespace Cavern.Format.Consts {
    /// <summary>
    /// Constant values used for Material eXchange Format.
    /// </summary>
    internal static class MXFConsts {
        /// <summary>
        /// The first 4 bytes of every MXF <see cref="KeyLengthValueSMPTE"/>. Also the magic number for an MXF file.
        /// </summary>
        public const int universalLabel = 0x342B0E06;

        /// <summary>
        /// The first 4 bytes of every MXF <see cref="KeyLengthValueSMPTE"/> as it's stored in the memory.
        /// </summary>
        public const int universalLabelLE = 0x060E2B34;

        /// <summary>
        /// Registry header of value dictionaries.
        /// </summary>
        public const int packRegistry = 0x02050101;

        /// <summary>
        /// Registry header of immersive audio elements.
        /// </summary>
        public const int immersiveAudioRegistry = 0x01020105;

        /// <summary>
        /// Marks an immersive audio essence block.
        /// </summary>
        public const ulong immersiveAudioEssence = 0x0E09060100000001;
    }
}