namespace Cavern.Format.Common {
    /// <summary>
    /// Container formats supported by Cavern.
    /// </summary>
    public enum Container {
        /// <summary>
        /// The format is not a container, but a simple audio file.
        /// </summary>
        NotContainer,
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
    }
}