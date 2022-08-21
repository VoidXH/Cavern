using System;
using System.IO;

using Cavern.Format.Common;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Connects RIFF WAVE channels with <see cref="AudioDefinitionModel"/> tracks.
    /// </summary>
    public sealed class ChannelAssignment {
        /// <summary>
        /// The parsed channel assignment.
        /// </summary>
        public Tuple<short, string>[] Assignment { get; private set; }

        /// <summary>
        /// Read the channel assignment from an ADM BWF file's related chunk.
        /// </summary>
        public ChannelAssignment(Stream reader) {
            short count = reader.ReadInt16();
            reader.Position += 2; // Count again

            Assignment = new Tuple<short, string>[count];
            for (short i = 0; i < count; i++) {
                Assignment[i] = new Tuple<short, string>(reader.ReadInt16(), reader.ReadCString());
            }
        }
    }
}