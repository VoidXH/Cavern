using System;
using System.IO;
using System.Text;

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
        public long Length { get; private set; }

        /// <summary>
        /// Position in the file where the raw data of this entry starts.
        /// </summary>
        protected readonly long position;

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
        public byte[] GetBytes(BinaryReader reader) {
            reader.BaseStream.Position = position;
            return reader.ReadBytes((int)Length);
        }

        /// <summary>
        /// Read the value as a big-endian float.
        /// </summary>
        public double GetFloatBE(BinaryReader reader) {
            byte[] bytes = GetBytes(reader);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            if (Length == 8)
                return BitConverter.ToDouble(bytes);
            return BitConverter.ToSingle(bytes);
        }

        /// <summary>
        /// Read the value as an UTF-8 string.
        /// </summary>
        public string GetUTF8(BinaryReader reader) => Encoding.UTF8.GetString(GetBytes(reader));

        /// <summary>
        /// Read the value as <see cref="VarInt"/>.
        /// </summary>
        public long GetValue(BinaryReader reader) {
            reader.BaseStream.Position = position;
            return VarInt.ReadValue(reader, (int)Length);
        }

        /// <summary>
        /// Advance the file to the next KLV.
        /// </summary>
        public void Skip(BinaryReader reader) => reader.BaseStream.Seek(Length, SeekOrigin.Current);
    }
}