using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Track metadata block of an MP4 container.
    /// </summary>
    /// <see href="https://developer.apple.com/library/archive/documentation/QuickTime/QTFF/QTFFChap2/qtff2.html"/>
    internal class TrackBox : NestedBox {
        /// <summary>
        /// Partially parsed track metadata. Has to be filled by the root parser.
        /// </summary>
        public Track Track { get; }

        /// <summary>
        /// Track metadata block of an MP4 container.
        /// </summary>
        public TrackBox(uint length, Stream reader) : base(length, trackBox, reader) {
            Box headerBox = this[trackHeaderBox];
            if (headerBox == null) {
                ThrowCorruption(trackHeaderBox);
            }
            NestedBox mediaMeta = (NestedBox)this[mediaBox];
            if (mediaMeta == null) {
                ThrowCorruption(mediaBox);
            }

            byte[] trackHeader = headerBox.GetRawData(reader);
            Track = new Track(null, trackHeader.ReadInt32BE(12));

            byte[] mediaHeader = mediaMeta[mediaHeaderBox]?.GetRawData(reader);
            if (mediaHeader == null) {
                ThrowCorruption(mediaHeaderBox);
            }
            LanguageCode languageCode = (LanguageCode)mediaHeader.ReadUInt16BE(20);
            if (languageCode < LanguageCode.Unspecified) {
                Track.Language = (languageCode).ToString();
            }

            uint timeScale = mediaHeader.ReadUInt32BE(12);
            if (((mediaMeta[mediaInfoBox] as NestedBox)?[sampleTableBox] as NestedBox)?[sampleDescriptionBox] is
                SampleDescriptionBox stsd && stsd.Formats.Length == 1) {
                Track.Format = stsd.Formats[0].codec;
                if (Track.Format.IsAudio()) {
                    byte[] extra = stsd.Formats[0].extra;
                    Track.Extra = new TrackExtraAudio() {
                        Bits = (BitDepth)extra.ReadInt16(11),
                        ChannelCount = extra.ReadInt16(9),
                        SampleRate = timeScale
                    };
                }
            }
        }
    }
}