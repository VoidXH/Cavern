using Cavern.Remapping;

namespace Cavern.Format.Consts {
    /// <summary>
    /// Used for both <see cref="RIFFWaveReader"/> and <see cref="RIFFWaveWriter"/>.
    /// </summary>
    static class RIFFWave {
        /// <summary>
        /// RIFF sync word, stream marker.
        /// </summary>
        public const int syncWord1 = 0x46464952;

        /// <summary>
        /// RF64 sync word, 64-bit stream marker.
        /// </summary>
        public const int syncWord1_64 = 0x34364652;

        /// <summary>
        /// WAVE sync word, specific header section marker.
        /// </summary>
        public const int syncWord2 = 0x45564157;

        /// <summary>
        /// fmt sync word, standard header marker.
        /// </summary>
        public const int formatSync = 0x20746D66;

        /// <summary>
        /// JUNK sync word, ADM BWF header marker.
        /// </summary>
        public const int junkSync = 0x4B4E554A;

        /// <summary>
        /// ds64 sync word, contains 64-bit lengths.
        /// </summary>
        public const int ds64Sync = 0x34367364;

        /// <summary>
        /// axml sync word, ADM XML metadata marker.
        /// </summary>
        public const int axmlSync = 0x6C6D7861;

        /// <summary>
        /// Data header marker.
        /// </summary>
        public const int dataSync = 0x61746164;

        /// <summary>
        /// Meaning of each bit in WAVEFORMATEXTENSIBLE's channel mask.
        /// </summary>
        public static readonly ReferenceChannel[] channelMask = {
            ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight, ReferenceChannel.FrontCenter, ReferenceChannel.ScreenLFE,
            ReferenceChannel.RearLeft, ReferenceChannel.RearRight,
            ReferenceChannel.FrontLeftCenter, ReferenceChannel.FrontRightCenter, ReferenceChannel.RearCenter,
            ReferenceChannel.SideLeft, ReferenceChannel.SideRight, ReferenceChannel.GodsVoice,
            ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight,
            ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight
        };
    }
}