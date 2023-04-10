using System.IO;

using Cavern.Utilities;

namespace Cavern.Format.Common {
    /// <summary>
    /// Variable-size integer ("vint"). Vints work by having as many leading zeros as extra bytes are used.
    /// In 4 bytes, a maximum of 28 bits can be written, as the first 3 bits are 0, marking the 3 extra bytes,
    /// and another 1 has to close the sequence of 0s.
    /// </summary>
    public static class VarInt {
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
        /// <summary>
        /// Reads the next VINT from a stream, does not cut the leading 1.
        /// </summary>
        public static long ReadTag(Stream reader) {
            int first = reader.ReadByte();
            int extraBytes = QMath.LeadingZerosInByte(first);
            long value = first;
            for (int i = 0; i < extraBytes; i++) {
                value = (value << 8) | reader.ReadByte();
            }
            return value;
        }

        /// <summary>
        /// Reads the next VINT from a stream, cuts the leading 1, reads the correct value.
        /// </summary>
        public static long ReadValue(Stream reader) {
            long value = ReadTag(reader);
            return value - (1L << QMath.BitsAfterMSB(value));
        }

        /// <summary>
        /// Reads the next signed VINT from a stream, cuts the leading 1, reads the correct value.
        /// </summary>
        public static long ReadSignedValue(Stream reader) {
            long value = ReadTag(reader);
            return value - (3L << (QMath.BitsAfterMSB(value) - 1)) + 1;
        }

        /// <summary>
        /// Reads a fixed length VINT (the actual value field from a <see cref="KeyLengthValue"/>).
        /// </summary>
        public static long ReadValue(Stream reader, int length) {
            long value = 0;
            for (int i = 0; i < length; i++) {
                value = (value << 8) | reader.ReadByte();
            }
            return value;
        }
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
        /// <summary>
        /// Create a placeholder that will later be filled with a calculated value.
        /// </summary>
        public static void Prepare(Stream writer, byte bytes) {
            writer.WriteByte((byte)(1L << (8 - bytes)));
            for (int i = 1; i < bytes; i++) {
                writer.WriteByte(0);
            }
        }

        /// <summary>
        /// When the <paramref name="writer"/> is moved back to the position of a prepared tag, overwrite it with the correct value.
        /// </summary>
        public static void Fill(Stream writer, byte bytes, long value) {
            value += 1L << ((bytes << 3) - bytes); // Leading 1 after the 0s that count the bytes
            int remains = bytes << 3;
            while ((remains -= 8) >= 0) {
                writer.WriteByte((byte)(value >> remains));
            }
        }

        /// <summary>
        /// Write an integer to exactly as many bytes as it requires.
        /// </summary>
        public static void Write(Stream writer, int value) => Fill(writer, (byte)((31 - QMath.LeadingZeros(value)) / 7 + 1), value);

        /// <summary>
        /// Write a tag that already contains the leading 1, but in its last byte, since they are stored as little-endian internally.
        /// </summary>
        /// <remarks>These values can directly be written to the stream after the 0 bytes are cut.</remarks>
        public static void WriteTag(Stream writer, int tag) {
            int remains = (4 - (QMath.LeadingZeros(tag) >> 3)) << 3;
            while ((remains -= 8) >= 0) {
                writer.WriteByte((byte)(tag >> remains));
            }
        }
    }
}