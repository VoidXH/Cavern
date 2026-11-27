namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Bass curve options available for room curves.
    /// </summary>
    public enum RoomCurveBassType {
        /// <summary>
        /// The customizable slope and knee found in Cavern.
        /// </summary>
        Cavern,

        /// <summary>
        /// Same as using a lowshelf, but generating the curve points for it.
        /// </summary>
        Shelving
    }
}
