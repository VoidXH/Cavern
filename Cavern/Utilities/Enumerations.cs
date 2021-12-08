namespace Cavern {
    /// <summary>
    /// Relations of PC jack outputs and their channels.
    /// </summary>
    public enum Jack {
        /// <summary>
        /// Front Jack (channel 0-1).
        /// </summary>
        Front = 0,
        /// <summary>
        /// Center/LFE Jack (channel 2-3).
        /// </summary>
        CenterLFE = 2,
        /// <summary>
        /// Rear (or Side in 5.1/Side configuration) Jack (channel 4-5).
        /// </summary>
        Rear = 4,
        /// <summary>
        /// Side Jack (channel 6-7).
        /// </summary>
        Side = 6
    }

    /// <summary>Cavern processing quality levels.</summary>
    public enum QualityModes {
        /// <summary>
        /// Lowest quality: forced maximum performance by disabling some features.
        ///
        /// Doppler effect and pitch shifting: No.
        /// Resampling quality: Low (nearest neighbour).
        /// Channels mixed to 3D space: First.
        /// Accurate angle calculation in asymmetric mode: No.
        /// Non-approximated 3D mix for Studio/Home in asymmetric mode: No.
        /// </summary>
        Low = 0,
        /// <summary>
        /// Medium quality: maximum performance with all features enabled.
        ///
        /// Doppler effect and pitch shifting: Low quality.
        /// Resampling quality: Low (nearest neighbour).
        /// Channels mixed to 3D space: First.
        /// Accurate angle calculation in asymmetric mode: No.
        /// Non-approximated 3D mix for Studio/Home in asymmetric mode: No.
        /// </summary>
        Medium,
        /// <summary>
        /// High quality: maximum quality except for heavy features.
        ///
        /// Doppler effect and pitch shifting: High quality.
        /// Resampling quality: Medium (linear interpolation).
        /// Channels mixed to 3D space: All.
        /// Accurate angle calculation in asymmetric mode: Yes.
        /// Non-approximated 3D mix for Studio/Home in asymmetric mode: No.
        /// </summary>
        High,
        /// <summary>
        /// Perfect quality: maximum quality for all features.
        ///
        /// Doppler effect and pitch shifting: High quality.
        /// Resampling quality: High (Catmull-Rom).
        /// Channels mixed to 3D space: All.
        /// Accurate angle calculation in asymmetric mode: Yes.
        /// Non-approximated 3D mix for Studio/Home in asymmetric mode: Yes.
        /// </summary>
        Perfect
    }

    /// <summary>
    /// Audio rolloff modes.
    /// </summary>
    public enum Rolloffs {
        /// <summary>
        /// Logarithmic rolloff by distance.
        /// </summary>
        Logarithmic,
        /// <summary>
        /// Linear rolloff in range.
        /// </summary>
        Linear,
        /// <summary>
        /// Physically correct rolloff by distance.
        /// </summary>
        Real,
        /// <summary>
        /// No rolloff.
        /// </summary>
        Disabled
    }

    /// <summary>
    /// Environment type, which determines rendering method.
    /// </summary>
    public enum Environments {
        /// <summary>
        /// For a single listener in the center with speakers placed around in a sphere.<br />
        /// <b>Symmetric engine</b>: balance-based.<br />
        /// <b>Asymmetric engine</b>: hybrid directional.
        /// </summary>
        Studio = 0,

        /// <summary>
        /// For a single listener or a few listeners close to each other on the center with
        /// speakers placed around in a cuboid.<br />
        /// <b>Symmetric engine</b>: balance-based.<br />
        /// <b>Asymmetric engine</b>: hybrid distance-based.
        /// </summary>
        Home = 1,

        /// <summary>
        /// For many listeners. Viewers at the sides or the back of the room will also
        /// experience 3D audio, unlike in Studio or Home environments, but this will reduce
        /// the overall effect quality, even on the center.<br />
        /// <b>Symmetric engine</b>: balance-based.<br />
        /// <b>Asymmetric engine</b>: directional.
        /// </summary>
        Theatre = 2
    }
}