using System.IO;

namespace Cavern.Format.Common {
    /// <summary>
    /// A tag and its data encoded in the format of a key, a length, and a value, most notably used in the EBML format.
    /// </summary>
    /// <remarks><see cref="BinaryReader"/> is not cached. This is an intentional memory optimization and has to be
    /// treated carefully.</remarks>
    internal class KeyLengthValue {
        /// <summary>
        /// Key of the entry.
        /// </summary>
        public int Tag { get; private set; }

        /// <summary>
        /// Length of the entry.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Position in the file where the raw data of this entry starts.
        /// </summary>
        readonly long position;

        /// <summary>
        /// Read the metadata of a KLV.
        /// </summary>
        public KeyLengthValue(BinaryReader reader) {
            Tag = (int)VarInt.ReadTag(reader);
            Length = VarInt.ReadValue(reader);
            if (Length < 0)
                Length = 0;
            position = reader.BaseStream.Position;
        }

        /// <summary>
        /// Read the raw bytes of the value.
        /// </summary>
        public byte[] GetRawData(BinaryReader reader) {
            reader.BaseStream.Position = position;
            return reader.ReadBytes(Length);
        }

        /// <summary>
        /// Read the value as <see cref="VarInt"/>.
        /// </summary>
        public int GetValue(BinaryReader reader) {
            reader.BaseStream.Position = position;
            return VarInt.ReadValue(reader);
        }

        /// <summary>
        /// Advance the file to the next KLV.
        /// </summary>
        public void Skip(BinaryReader reader) => reader.BaseStream.Seek(Length, SeekOrigin.Current);
    }
}