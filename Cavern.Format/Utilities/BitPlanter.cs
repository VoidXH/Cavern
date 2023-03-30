using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Write a stream with custom-sized blocks, down to the bit. This is initially written to a temporary cache,
    /// which has to be written to a byte-based stream with <see cref="WriteToStream(Stream)"/>.
    /// </summary>
    sealed class BitPlanter {
        /// <summary>
        /// Total number of significant bits in the <see cref="cache"/>.
        /// </summary>
        public int BitsWritten => currentByte * 8 + currentBit;

        /// <summary>
        /// Auto-resized holder of the data.
        /// </summary>
        /// <remarks>Reallocation happens in <see cref="NextByte"/>.</remarks>
        byte[] cache = new byte[32];

        /// <summary>
        /// The index of <see cref="cache"/> written next.
        /// </summary>
        int currentByte;

        /// <summary>
        /// The bit of the <see cref="currentByte"/> written next.
        /// </summary>
        int currentBit;

        /// <summary>
        /// Get the CRC value of the written stream.
        /// </summary>
        public int CalculateCRC16(int fromBit, int polynomial) {
            BitExtractor extractor = new BitExtractor(cache);
            extractor.Skip(fromBit);
            int end = BitsWritten,
                crc = 0;
            while (extractor.Position < end) {
                if (extractor.ReadBit() != (((crc & 0x8000) >> 15) == 1)) {
                    crc = (crc << 1) ^ polynomial;
                } else {
                    crc <<= 1;
                }
            }
            return crc & 0xFFFF;
        }

        /// <summary>
        /// Write a <paramref name="value"/> at a specific <paramref name="offset"/>
        /// from the start of a length in <paramref name="bits"/>.
        /// </summary>
        public void Overwrite(int offset, int value, int bits) {
            int writtenByte = offset >> 3;
            offset &= 7;
            while (bits > 0) {
                int bitsToWrite = Math.Min(8 - offset, bits);
                cache[writtenByte] = (byte)((cache[writtenByte] << bitsToWrite) +
                    ((value >> (bits - bitsToWrite)) & ((1 << bitsToWrite) - 1)));
                bits -= bitsToWrite;
                offset += bitsToWrite;
                if (offset == 8) {
                    offset = 0;
                }
            }
        }

        /// <summary>
        /// Append a flag to the stream under construction.
        /// </summary>
        public void Write(bool value) {
            cache[currentByte] = (byte)((cache[currentByte] << 1) + (value ? 1 : 0));
            if (++currentBit == 8) {
                NextByte();
            }
        }

        /// <summary>
        /// Append an array of bytes to the stream under construction, without aligning to byte borders.
        /// The length is given in bytes.
        /// </summary>
        public void Write(byte[] value, int length) {
            for (int i = 0; i < length; i++) {
                Write(value[i], 8);
            }
        }

        /// <summary>
        /// Append a custom width value to the stream under construction.
        /// </summary>
        public void Write(int value, int bits) {
            while (bits > 0) {
                int bitsToWrite = Math.Min(8 - currentBit, bits);
                cache[currentByte] = (byte)((cache[currentByte] << bitsToWrite) +
                    ((value >> (bits - bitsToWrite)) & ((1 << bitsToWrite) - 1)));
                bits -= bitsToWrite;
                currentBit += bitsToWrite;
                if (currentBit == 8) {
                    NextByte();
                }
            }
        }

        /// <summary>
        /// Append a custom width conditional value to the stream under construction.
        /// </summary>
        public void Write(int? value, int bits) {
            Write(value.HasValue);
            if (value != null) {
                Write(value.Value, bits);
            }
        }

        /// <summary>
        /// Append an array of bytes to the stream under construction, without aligning to byte borders.
        /// The length is given in bits.
        /// </summary>
        public void WriteBits(byte[] value, int length) {
            int fullBytes = length >> 3;
            Write(value, fullBytes);
            int remainder = length & 7;
            if (remainder != 0) {
                Write(value[fullBytes], remainder);
            }
        }

        /// <summary>
        /// Write the constructed bitstream to a bytestream and reset this instance.
        /// </summary>
        public void WriteToStream(Stream target) {
            int bytesToWrite = currentByte;
            if (currentBit != 0 || currentByte == 0) {
                cache[currentByte] <<= 8 - currentBit;
                ++bytesToWrite;
            }
            target.Write(cache, 0, bytesToWrite);
            Array.Clear(cache, 0, cache.Length);
            currentByte = 0;
            currentBit = 0;
        }

        /// <summary>
        /// A byte was fully written, jump to the next one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void NextByte() {
            if (++currentByte == cache.Length) {
                Array.Resize(ref cache, cache.Length + (cache.Length >> 1)); // resize factor: 1.5
            }
            currentBit = 0;
        }
    }
}