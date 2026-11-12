namespace Cavern.Utilities.Enums {
    /// <summary>
    /// Resampling algorithm selection.
    /// </summary>
    public enum ResampleAlgorithm {
        /// <summary>
        /// Nearest neighbour - fast, low quality.
        /// </summary>
        NearestNeighbour = 0,

        /// <summary>
        /// Linear interpolation - medium quality.
        /// </summary>
        Lerp = 1,

        /// <summary>
        /// Catmull-Rom spline - high quality.
        /// </summary>
        CatmullRom = 2
    }
}
