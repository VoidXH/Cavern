using System.Text;

namespace Cavern.Channels {
    /// <summary>
    /// Possible channels in channel-based legacy systems.
    /// </summary>
    /// <remarks>The standard 7.1 layout is the first 8 entries. When you update this, don't forget:<br />
    /// - Cavern.Format.Renderers.Renderer<br />
    /// - Cavern.Format.Transcoders.AudioDefinitionModelElements.ADMConsts<br />
    /// - <see cref="ChannelPrototype.Mapping"/></remarks>
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
        Unknown,
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
        TopRearRight,
        TopRearCenter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Extension functions for reference channels.
    /// </summary>
    public static class ReferenceChannelExtensions {
        /// <summary>
        /// Get the first letters of each word in the channel's name, like TFL from Top Front Left.
        /// </summary>
        public static string GetShortName(this ReferenceChannel channel) {
            StringBuilder result = new StringBuilder();
            string source = channel.ToString();
            for (int i = channel == ReferenceChannel.ScreenLFE ? 6 : 0; i < source.Length; i++) {
                if ('A' <= source[i] && source[i] <= 'Z') {
                    result.Append(source[i]);
                }
            }
            return result.ToString();
        }
    }
}