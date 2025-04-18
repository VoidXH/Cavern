using System.IO;

using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Box of track cuts and timing changes.
    /// </summary>
    internal class EditListBox : Box {
        /// <summary>
        /// Movie to media time mapping:<br/>
        /// - trackDuration: length in movie time scale units,<br/>
        /// - mediaTime: starting time or -1 if the edit is empty<br/>,
        /// - mediaRate: time scale multiplier.
        /// </summary>
        public readonly (uint trackDuration, int mediaTime, float mediaRate)[] edits;

        /// <summary>
        /// Box of track cuts and timing changes.
        /// </summary>
        public EditListBox(uint length, Stream reader) : base(length, editListBox, reader) {
            reader.Position += 4; // Version byte and zero flags
            edits = new (uint, int, float)[reader.ReadUInt32BE()];
            for (int i = 0; i < edits.Length; i++) {
                uint trackDuration = reader.ReadUInt32BE();
                int mediaTime = reader.ReadInt32BE();
                ushort mediaRateInteger = reader.ReadUInt16BE();
                ushort mediaRateFraction = reader.ReadUInt16BE();
                edits[i] = (trackDuration, mediaTime, mediaRateInteger + mediaRateFraction / 65536f);
            }
        }
    }
}