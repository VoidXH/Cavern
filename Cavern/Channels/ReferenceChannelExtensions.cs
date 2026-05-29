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
        /// Get the channel's DCI-standard shortened name, and if it not exists, fall back to <see cref="GetShortName"/>.
        /// </summary>
        public static string GetShortNameDCI(this ReferenceChannel channel) => channel switch {
            ReferenceChannel.FrontLeft => ChannelPrototype.frontLeftMark,
            ReferenceChannel.FrontRight => ChannelPrototype.frontRightMark,
            ReferenceChannel.FrontCenter => ChannelPrototype.frontCenterMark,
            ReferenceChannel.ScreenLFE => ChannelPrototype.screenLFEMark,
            ReferenceChannel.RearLeft => ChannelPrototype.rearLeftMarkDCI,
            ReferenceChannel.RearRight => ChannelPrototype.rearRightMarkDCI,
            ReferenceChannel.SideLeft => ChannelPrototype.sideLeftMarkDCI,
            ReferenceChannel.SideRight => ChannelPrototype.sideRightMarkDCI,
            ReferenceChannel.TopSideLeft => ChannelPrototype.topSideLeftMarkDCI,
            ReferenceChannel.TopSideRight => ChannelPrototype.topSideRightMarkDCI,
            _ => channel.GetShortName()
        };

        /// <summary>
        /// Is the parameter a height <paramref name="channel"/>?
        /// </summary>
        public static bool IsHeight(this ReferenceChannel channel) =>
            (channel >= ReferenceChannel.TopFrontLeft && channel <= ReferenceChannel.TopSideRight) ||
            channel == ReferenceChannel.TopFrontCenter || channel == ReferenceChannel.GodsVoice ||
            (channel >= ReferenceChannel.TopRearLeft && channel <= ReferenceChannel.TopRearCenter);
    }
}
