using System;
using System.Collections.Generic;

using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Utilities;

namespace Cavern.Format.Consts {
    /// <summary>
    /// Used for both <see cref="RIFFWaveReader"/> and <see cref="RIFFWaveWriter"/>.
    /// </summary>
    static class RIFFWave {
        /// <summary>
        /// Bit masks for channels present in a WAV file.
        /// </summary>
        [Flags]
        public enum WaveExtensibleChannel {
            /// <summary>
            /// Marks a channel that can't be part of a channel mask.
            /// </summary>
            None,
            FrontLeft = 0x1,
            FrontRight = 0x2,
            FrontCenter = 0x4,
            LowFrequencyEffects = 0x8,
            BackLeft = 0x10,
            BackRight = 0x20,
            FrontLeftCenter = 0x40,
            FrontRightCenter = 0x80,
            BackCenter = 0x100,
            SideLeft = 0x200,
            SideRight = 0x400,
            GodsVoice = 0x800,
            TopFrontLeft = 0x1000,
            TopFrontCenter = 0x2000,
            TopFrontRight = 0x4000,
            TopBackLeft = 0x8000,
            TopBackCenter = 0x10000,
            TopBackRight = 0x20000,
            /// <summary>
            /// A channel that's not even valid as a <see cref="ReferenceChannel"/>.
            /// </summary>
            Unknown = 0x40000
        }

        /// <summary>
        /// Assigns an array of <see cref="ReferenceChannel"/>s to a channel mask while checking for consistency.
        /// </summary>
        /// <remarks>Using an unknown channel results in skipping the channel mapping on export.</remarks>
        public static int CreateChannelMask(ReferenceChannel[] channels) {
            int result = 0;
            List<ReferenceChannel> illegalChannels = null;
            for (int i = 0; i < channels.Length; i++) {
                int mapped = (int)extensibleChannelMapping[(int)channels[i]];
                if (mapped == (int)WaveExtensibleChannel.None) {
                    illegalChannels ??= new List<ReferenceChannel>();
                    illegalChannels.Add(channels[i]);
                } else if (mapped == (int)WaveExtensibleChannel.Unknown) {
                    return -1;
                }
                if ((result & mapped) != 0) {
                    throw new DuplicateChannelException();
                }
                result |= mapped;
            }

            if (illegalChannels != null) {
                throw new InvalidChannelException(illegalChannels.ToArray());
            }

            for (int i = 1; i < channels.Length; i++) {
                if (extensibleChannelMapping[(int)channels[i - 1]] > extensibleChannelMapping[(int)channels[i]]) {
                    throw new InvalidChannelOrderException();
                }
            }

            return result;
        }

        /// <summary>
        /// Gets which channels and in what order are part of a WAVE file by channel mask.
        /// </summary>
        public static ReferenceChannel[] ParseChannelMask(int mask) {
            ReferenceChannel[] result = new ReferenceChannel[QMath.PopulationCount(mask)];
            Array supportedChannels = Enum.GetValues(typeof(WaveExtensibleChannel));
            int channel = 0;
            for (int ch = 0; ch < supportedChannels.Length; ch++) {
                WaveExtensibleChannel checkedChannel = (WaveExtensibleChannel)supportedChannels.GetValue(ch);
                if ((mask & (int)checkedChannel) != 0) {
                    for (int j = 0; j < extensibleChannelMapping.Length; j++) {
                        if (extensibleChannelMapping[j] == checkedChannel) {
                            result[channel++] = (ReferenceChannel)j;
                            break;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// RIFF sync word, stream marker.
        /// </summary>
        public const int syncWord1 = 0x46464952;

        /// <summary>
        /// RF64 sync word, 64-bit stream marker.
        /// </summary>
        public const int syncWord1_64 = 0x34364652;

        /// <summary>
        /// WAVE sync word, specific header section marker.
        /// </summary>
        public const int syncWord2 = 0x45564157;

        /// <summary>
        /// fmt sync word, standard header marker.
        /// </summary>
        public const int formatSync = 0x20746D66;

        /// <summary>
        /// JUNK sync word, ADM BWF header marker.
        /// </summary>
        public const int junkSync = 0x4B4E554A;

        /// <summary>
        /// ds64 sync word, contains 64-bit lengths.
        /// </summary>
        public const int ds64Sync = 0x34367364;

        /// <summary>
        /// axml sync word, ADM XML metadata marker.
        /// </summary>
        public const int axmlSync = 0x6C6D7861;

        /// <summary>
        /// chna sync word, channel assignment to the AXML.
        /// </summary>
        public const int chnaSync = 0x616E6863;

        /// <summary>
        /// Data header marker.
        /// </summary>
        public const int dataSync = 0x61746164;

        /// <summary>
        /// Dolby audio Metadata chunk marker.
        /// </summary>
        public const int dbmdSync = 0x646d6264;

        /// <summary>
        /// Converts the <see cref="ReferenceChannel"/> values to a <see cref="WaveExtensibleChannel"/>.
        /// </summary>
        public static readonly WaveExtensibleChannel[] extensibleChannelMapping = {
            WaveExtensibleChannel.FrontLeft,
            WaveExtensibleChannel.FrontRight,
            WaveExtensibleChannel.FrontCenter,
            WaveExtensibleChannel.LowFrequencyEffects,
            WaveExtensibleChannel.BackLeft,
            WaveExtensibleChannel.BackRight,
            WaveExtensibleChannel.SideLeft,
            WaveExtensibleChannel.SideRight,
            WaveExtensibleChannel.FrontLeftCenter,
            WaveExtensibleChannel.FrontRightCenter,
            WaveExtensibleChannel.None,
            WaveExtensibleChannel.None,
            WaveExtensibleChannel.Unknown,
            WaveExtensibleChannel.None,
            WaveExtensibleChannel.None,
            WaveExtensibleChannel.TopFrontLeft,
            WaveExtensibleChannel.TopFrontRight,
            WaveExtensibleChannel.None,
            WaveExtensibleChannel.None,
            WaveExtensibleChannel.None,
            WaveExtensibleChannel.None,
            WaveExtensibleChannel.TopFrontCenter,
            WaveExtensibleChannel.GodsVoice,
            WaveExtensibleChannel.BackCenter,
            WaveExtensibleChannel.None,
            WaveExtensibleChannel.None,
            WaveExtensibleChannel.TopBackLeft,
            WaveExtensibleChannel.TopBackRight,
            WaveExtensibleChannel.TopBackCenter
        };
    }
}