namespace Cavern.Format.Common {
    /// <summary>
    /// Container formats supported by Cavern.
    /// </summary>
    public enum Container {
        /// <summary>
        /// Used for marking that a file is not a container, but a simple audio file. Used depending on the use-case.
        /// </summary>
        NotContainer,

        // AV containers with multiple possible tracks
        /// <summary>
        /// Matroska and WebM.
        /// </summary>
        Matroska,
        /// <summary>
        /// MP4 and QuickTime.
        /// </summary>
        MP4,
        /// <summary>
        /// Material eXchange Format.
        /// </summary>
        MXF,

        // Audio-only containers
        /// <summary>
        /// LAF files for spatial PCM containment.
        /// </summary>
        Limitless,
        /// <summary>
        /// WAV files for PCM (+ ADM) containment.
        /// </summary>
        RIFFWave,
        /// <summary>
        /// CAF files for PCM containment.
        /// </summary>
        CoreAudio,
    }
}