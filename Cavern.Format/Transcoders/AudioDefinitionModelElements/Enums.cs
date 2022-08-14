namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Supported <see cref="ADMPackFormat"/> types.
    /// </summary>
    public enum ADMPackType {
        /// <summary>
        /// For channel-based audio, where each channel feeds a speaker directly.
        /// </summary>
        DirectSpeakers = 1,
        /// <summary>
        /// For channel-based audio where channels are matrixed together, such as Mid-Side, Lt/Rt.
        /// </summary>
        Matrix,
        /// <summary>
        /// For object-based audio where channels represent audio objects and position updates are provided.
        /// </summary>
        Objects,
        /// <summary>
        /// For scene-based audio where Ambisonics and HOA are used.
        /// </summary>
        HOA,
        /// <summary>
        /// For binaural audio, where playback is over headphones.
        /// </summary>
        Binaural
    }
}