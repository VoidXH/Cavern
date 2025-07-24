namespace Cavern.Format.Consts {
    /// <summary>
    /// Constants for reading/writing Core Audio Format files.
    /// </summary>
    internal class CoreAudioFormatConsts {
        /// <summary>
        /// caff sync word, stream marker.
        /// </summary>
        public const int syncWord = 0x66666163;

        /// <summary>
        /// desc sync word, audio description chunk marker.
        /// </summary>
        public const int audioDescriptionChunk = 0x63736564;
    }
}
