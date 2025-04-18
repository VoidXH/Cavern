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
        public MP4Track Track { get; }

        /// <summary>
        /// Track metadata block of an MP4 container.
        /// </summary>
        public TrackBox(uint length, Stream reader) : base(length, trackBox, reader) {
            Box headerBox = this[trackHeaderBox] ??
                throw new MissingElementException(trackHeaderBox.ToFourCC(), position);
            byte[] trackHeader = headerBox.GetRawData(reader);

            NestedBox mediaMeta = (NestedBox)this[mediaBox] ??
                throw new MissingElementException(mediaBox.ToFourCC(), position);
            byte[] mediaHeader = (mediaMeta[mediaHeaderBox]?.GetRawData(reader)) ??
                throw new MissingElementException(mediaHeaderBox.ToFourCC(), position);

            NestedBox sampleTable = (mediaMeta[mediaInfoBox] as NestedBox)?[sampleTableBox] as NestedBox;
            Track = new MP4Track(trackHeader, mediaHeader, sampleTable);
        }
    }
}