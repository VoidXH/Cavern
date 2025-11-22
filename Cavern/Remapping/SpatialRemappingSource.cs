namespace Cavern.Remapping {
    /// <summary>
    /// Content layouts supported for the <see cref="SpatialRemapping"/> feature.
    /// </summary>
    public enum SpatialRemappingSource {
        /// <summary>
        /// Spatial Remapping is disabled.
        /// </summary>
        Off,
        /// <summary>
        /// 7.1 with ITU angles (legacy content).
        /// </summary>
        ITU_7_1,
        /// <summary>
        /// 5.1.2 with ITU angles (legacy content).
        /// </summary>
        ITU_5_1_2,
        /// <summary>
        /// 7.1 with cinema standard room wall positions.
        /// </summary>
        Allocentric_7_1,
        /// <summary>
        /// 5.1.2 with cinema standard room wall positions.
        /// </summary>
        Allocentric_5_1_2
    }
}
