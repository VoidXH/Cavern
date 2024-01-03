using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Utilities;

namespace Cavern.Format.Container.MXF {
    /// <summary>
    /// A tag and its data encoded in the format of a key, a length, and a value, most notably used in the MXF format.
    /// </summary>
    /// <remarks><see cref="BinaryReader"/> is not cached. This is an intentional memory optimization and has to be
    /// treated carefully.</remarks>
    internal partial class KeyLengthValueSMPTE {
        /// <summary>
        /// Key of the entry.
        /// </summary>
        public (int marker, int registry, ulong item) Key { get; private set; }

        /// <summary>
        /// Length of the entry.
        /// </summary>
        public long Length { get; private set; }

        /// <summary>
        /// Position in the file where the raw data of this entry starts.
        /// </summary>
        protected readonly long position;

        /// <summary>
        /// Read the metadata of a KLV.
        /// </summary>
        protected KeyLengthValueSMPTE((int, int, ulong) key, Stream reader) {
            Key = key;
            Length = BasicEncodingRules.Read(reader);
            position = reader.Position;
        }

        /// <summary>
        /// Parse a KLV block in an object for its type.
        /// </summary>
        public static KeyLengthValueSMPTE Parse(Stream reader) {
            (int marker, int registry, ulong item) key = (reader.ReadInt32BE(), reader.ReadInt32BE(), reader.ReadUInt64BE());
            return key.registry switch {
                MXFConsts.packRegistry => new PackRegistry(key, reader),
                _ => new KeyLengthValueSMPTE(key, reader)
            };
        }

        /// <summary>
        /// Moves the <paramref name="reader"/> to the next KLV block.
        /// </summary>
        public void SeekToNext(Stream reader) => reader.Position = position + Length;
    }
}