namespace Cavern.Remapping {
    /// <summary>Possible channels in channel-based legacy systems.</summary>
    public enum ReferenceChannel {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        FrontLeft,
        FrontRight,
        FrontCenter,
        ScreenLFE,
        SideLeft,
        SideRight,
        RearLeft,
        RearRight,
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
        RearCenter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}