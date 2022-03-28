namespace Cavern.Remapping {
    /// <summary>
    /// Possible channels in channel-based legacy systems.
    /// </summary>
    /// <remarks>The standard 7.1 layout is the first 8 entries.</remarks>
    public enum ReferenceChannel {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        FrontLeft,
        FrontRight,
        FrontCenter,
        ScreenLFE,
        RearLeft,
        RearRight,
        SideLeft,
        SideRight,
        FrontLeftCenter,
        FrontRightCenter,
        HearingImpaired,
        VisuallyImpaired,
        Unused,
        MotionData,
        ExternalData,
        TopFrontLeft,
        TopFrontRight,
        TopSideLeft,
        TopSideRight,
        SignLanguage,
        BottomSurround,
        TopFrontCenter,
        GodsVoice,
        RearCenter,
        WideLeft,
        WideRight,
        TopRearLeft,
        TopRearRight
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}