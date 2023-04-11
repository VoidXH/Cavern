using System.Collections.Generic;
using System.IO;
using System.Text;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Container.Matroska {
    /// <summary>
    /// Creates a Matroska tree, handles layers, and writes values.
    /// </summary>
    internal class MatroskaTreeWriter {
        /// <summary>
        /// Output random access stream to write to. The length of each element is written after they're closed.
        /// </summary>
        readonly Stream writer;

        /// <summary>
        /// Creates a Matroska tree, handles layers, and writes values.
        /// </summary>
        public MatroskaTreeWriter(Stream writer) => this.writer = writer;

        /// <summary>
        /// Positions where sequence data starts, and their corresponding sequence length field sizes.
        /// </summary>
        readonly Stack<(long sizePosition, byte sizeBytes)> sequenceStarts = new Stack<(long sizePosition, byte sizeBytes)>();

        /// <summary>
        /// Open a new sequence in the current sequence.
        /// </summary>
        /// <param name="tag">KLV identifier of the sequence</param>
        /// <param name="sizeBytes">Number of bytes that can hold the maximum possible size of the sequence</param>
        public void OpenSequence(int tag, byte sizeBytes) {
            VarInt.WriteTag(writer, tag);
            VarInt.Prepare(writer, sizeBytes);
            sequenceStarts.Push((writer.Position, sizeBytes));
        }

        /// <summary>
        /// Write a tag that contains a single byte value.
        /// </summary>
        public void Write(int tag, byte value) {
            VarInt.WriteTag(writer, tag);
            writer.WriteByte(0x81); // Length of 1, 0 additional bytes
            writer.WriteByte(value);
        }

        /// <summary>
        /// Write a tag that contains raw bytes.
        /// </summary>
        public void Write(int tag, byte[] value) {
            VarInt.WriteTag(writer, tag);
            VarInt.Write(writer, value.Length);
            writer.Write(value, 0, value.Length);
        }

        /// <summary>
        /// Write a tag that contains a single short value.
        /// </summary>
        public void Write(int tag, short value) {
            VarInt.WriteTag(writer, tag);
            writer.WriteByte(0x82); // Length of 2, 0 additional bytes
            writer.WriteByte((byte)(value >> 8));
            writer.WriteByte((byte)value);
        }

        /// <summary>
        /// Write a tag that contains a single unsigned int value.
        /// </summary>
        public void Write(int tag, uint value) {
            VarInt.WriteTag(writer, tag);
            writer.WriteByte(0x84); // Length of 4, 0 additional bytes
            writer.WriteAny(value.ReverseEndianness());
        }

        /// <summary>
        /// Write a tag that contains a single unsigned long value.
        /// </summary>
        public void Write(int tag, ulong value) {
            VarInt.WriteTag(writer, tag);
            writer.WriteByte(0x88); // Length of 8, 0 additional bytes
            writer.WriteAny(value.ReverseEndianness());
        }

        /// <summary>
        /// Write a tag that contains a single floating point value.
        /// </summary>
        public void Write(int tag, float value) {
            VarInt.WriteTag(writer, tag);
            writer.WriteByte(0x84); // Length of 4, 0 additional bytes
            writer.WriteAny(new QMath.ConverterStruct { asFloat = value }.asUInt.ReverseEndianness());
        }

        /// <summary>
        /// Write a tag that contains a single string value.
        /// </summary>
        public void Write(int tag, string value) {
            VarInt.WriteTag(writer, tag);
            VarInt.Write(writer, value.Length);
            writer.Write(Encoding.ASCII.GetBytes(value));
        }

        /// <summary>
        /// Close the last opened sequence.
        /// </summary>
        public void CloseSequence() {
            long position = writer.Position;
            (long start, byte sizeBytes) = sequenceStarts.Pop();
            writer.Position = start - sizeBytes;
            VarInt.Fill(writer, sizeBytes, position - start);
            writer.Position = position;
        }
    }
}