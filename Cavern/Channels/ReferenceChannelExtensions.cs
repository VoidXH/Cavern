using System.Collections.Generic;
using System;
using System.Text;

namespace Cavern.Channels {
    /// <summary>
    /// Extension functions for reference channels.
    /// </summary>
    public static class ReferenceChannelExtensions {
        /// <summary>
        /// Unpack a set of <see cref="ReferenceChannel"/>s from a mask made with <see cref="GetMask(ReferenceChannel[])"/>.
        /// </summary>
        public static ReferenceChannel[] FromMask(int mask) {
            List<ReferenceChannel> result = new List<ReferenceChannel>();
            int channels = Enum.GetValues(typeof(ReferenceChannel)).Length;
            for (int i = 0; i < channels; i++) {
                if ((mask & (1 << i)) != 0) {
                    result.Add((ReferenceChannel)i);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Convert a set of <see cref="ReferenceChannel"/>s to a channel mask that can be decoded with <see cref="FromMask(int)"/>.
        /// </summary>
        public static int GetMask(this ReferenceChannel[] channels) {
            int result = 0;
            for (int i = 0; i < channels.Length; i++) {
                result |= 1 << (int)channels[i];
            }
            return result;
        }

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

        /// <summary>
        /// Is the parameter a height <paramref name="channel"/>?
        /// </summary>
        public static bool IsHeight(this ReferenceChannel channel) =>
            (channel >= ReferenceChannel.TopFrontLeft && channel <= ReferenceChannel.TopSideRight) ||
            channel == ReferenceChannel.TopFrontCenter || channel == ReferenceChannel.GodsVoice ||
            (channel >= ReferenceChannel.TopRearLeft && channel <= ReferenceChannel.TopRearCenter);

        /// <summary>
        /// Converts a standard channel shorthand to a <see cref="ReferenceChannel"/>.
        /// </summary>
        public static ReferenceChannel FromStandardName(string name) {
            switch (name) {
                case ChannelPrototype.frontLeftMark:
                case ChannelPrototype.frontLeftMarkFull:
                    return ReferenceChannel.FrontLeft;
                case ChannelPrototype.frontRightMark:
                case ChannelPrototype.frontRightMarkFull:
                    return ReferenceChannel.FrontRight;
                case ChannelPrototype.frontCenterMark:
                case ChannelPrototype.frontCenterMarkFull:
                    return ReferenceChannel.FrontCenter;
                case ChannelPrototype.screenLFEMark:
                case ChannelPrototype.subwooferMark:
                    return ReferenceChannel.ScreenLFE;
                case ChannelPrototype.rearLeftMark:
                    return ReferenceChannel.RearLeft;
                case ChannelPrototype.rearRightMark:
                    return ReferenceChannel.RearRight;
                case ChannelPrototype.sideLeftMark:
                    return ReferenceChannel.SideLeft;
                case ChannelPrototype.sideRightMark:
                    return ReferenceChannel.SideRight;
                default:
                    return ReferenceChannel.Unknown;
            }
        }
    }
}