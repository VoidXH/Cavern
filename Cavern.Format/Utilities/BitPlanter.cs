using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Write a stream with custom-sized blocks, down to the bit. This is initially written to a temporary cache,
    /// which has to be written to a byte-based stream with <see cref="WriteToStream(Stream)"/>.
    /// </summary>
    sealed class BitPlanter {
        byte[] cache = new byte[32];
        int currentByte = 0;
        int currentBit = 0;

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
            if (value != null) {
                Write(value.Value, bits);
            }
        }

        /// <summary>
        /// Export the constructed bitstream to a bytestream.
        /// </summary>
        /// <param name="target"></param>
        public void WriteToStream(Stream target) {
            int bytesToWrite = currentByte;
            if (currentBit != 0 || currentByte == 0) {
                cache[currentByte] <<= 8 - currentBit;
                ++bytesToWrite;
            }
            target.Write(cache, 0, bytesToWrite);
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