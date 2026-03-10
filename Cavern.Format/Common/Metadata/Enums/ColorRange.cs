namespace Cavern.Format.Common.Metadata.Enums {
    /// <summary>
    /// Used range of the available color values.
    /// </summary>
    public enum ColorRange {
        /// <summary>
        /// Use the default color range of the format.
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// 8 bits, 16-235.
        /// </summary>
        BroadcastRange = 1,
        /// <summary>
        /// Fully using all bits without clipping.
        /// </summary>
        FullRange = 2,
        /// <summary>
        /// Specified elsewhere.
        /// </summary>
        Other = 3
    }
}
