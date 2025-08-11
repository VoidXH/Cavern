using System.Collections.Generic;

using Cavern.Channels;
using Cavern.Format.Common;

namespace Cavern.Format.Decoders.MeridianLosslessPacking {
    // Functions for parsing single MLP header values.
    partial class MLPHeader {
        /// <summary>
        /// Interpret the sample rate from the given code.
        /// </summary>
        void ParseSampleRate(int code) {
            SampleRate = code switch {
                0 => 48000,
                1 => 96000,
                2 => 192000,
                8 => 44100,
                9 => 88200,
                10 => 176400,
                _ => throw new UnsupportedFeatureException("Sample rate " + code),
            };
        }

        /// <summary>
        /// Parse the channel mask to a <see cref="ReferenceChannel"/> set.
        /// </summary>
        ReferenceChannel[] ParseChannelMask(int mask) {
            List<ReferenceChannel> channels = new List<ReferenceChannel>();
            if ((mask & (1 << 0)) != 0) {
                channels.Add(ReferenceChannel.FrontLeft);
                channels.Add(ReferenceChannel.FrontRight);
            }
            if ((mask & (1 << 1)) != 0) {
                channels.Add(ReferenceChannel.FrontCenter);
            }
            if ((mask & (1 << 2)) != 0) {
                channels.Add(ReferenceChannel.ScreenLFE);
            }
            if ((mask & (1 << 3)) != 0) {
                channels.Add(ReferenceChannel.SideLeft);
                channels.Add(ReferenceChannel.SideRight);
            }
            if ((mask & (1 << 4)) != 0) {
                channels.Add(ReferenceChannel.TopFrontLeft);
                channels.Add(ReferenceChannel.TopFrontRight);
            }
            if ((mask & (1 << 5)) != 0) {
                channels.Add(ReferenceChannel.FrontLeftCenter);
                channels.Add(ReferenceChannel.FrontRightCenter);
            }
            if ((mask & (1 << 6)) != 0) {
                channels.Add(ReferenceChannel.RearLeft);
                channels.Add(ReferenceChannel.RearRight);
            }
            if ((mask & (1 << 7)) != 0) {
                channels.Add(ReferenceChannel.RearCenter);
            }
            if ((mask & (1 << 8)) != 0) {
                channels.Add(ReferenceChannel.GodsVoice);
            }
            if ((mask & (1 << 9)) != 0) { // Direct (point source) surrounds, but everything is direct in practice
                channels.Add(ReferenceChannel.SideLeft);
                channels.Add(ReferenceChannel.SideRight);
            }
            if ((mask & (1 << 10)) != 0) {
                channels.Add(ReferenceChannel.WideLeft);
                channels.Add(ReferenceChannel.WideRight);
            }
            if ((mask & (1 << 11)) != 0) {
                channels.Add(ReferenceChannel.TopFrontCenter);
            }
            if ((mask & (1 << 12)) != 0) {
                channels.Add(ReferenceChannel.ScreenLFE);
            }
            return channels.ToArray();
        }
    }
}
