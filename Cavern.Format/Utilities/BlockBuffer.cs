﻿using System;
using System.IO;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Converts a function that fetches a given chunk of a stream to an object that can fetch a block with any size.
    /// </summary>
    public sealed class BlockBuffer<T> {
        /// <summary>
        /// True if there's more data to read.
        /// </summary>
        public bool Readable => LastFetch != null;

        /// <summary>
        /// The <see cref="Seeker"/> is set, so it's possible to jump in stream.
        /// </summary>
        public bool Seekable => Seeker != null;

        /// <summary>
        /// Indicates that the <see cref="LastFetch"/> was not yet read from.
        /// </summary>
        public bool FreshFetch => lastFetchPosition == 0;

        /// <summary>
        /// The position of the first sample of the last exported block in the buffer.
        /// </summary>
        public int LastFetchStart { get; private set; }

        /// <summary>
        /// Result of the last <see cref="Fetcher"/> call.
        /// </summary>
        public T[] LastFetch { get; private set; }

        /// <summary>
        /// Reusable output array.
        /// </summary>
        T[] result = new T[0];

        /// <summary>
        /// First sample from <see cref="LastFetch"/> that wasn't collected.
        /// </summary>
        int lastFetchPosition;

        /// <summary>
        /// Calls when new block data is needed.
        /// </summary>
        event Func<T[]> Fetcher;

        /// <summary>
        /// Jumps to a position in the source stream if it's supported.
        /// </summary>
        event Action<long> Seeker;

        /// <summary>
        /// Converts a function that fetches a given chunk of a stream to an object that can fetch a block with any size.
        /// </summary>
        public BlockBuffer(Func<T[]> fetcher) : this(fetcher, null, fetcher()) { }

        /// <summary>
        /// Converts a function that fetches a given chunk of a stream to an object that can fetch a block with any size.
        /// </summary>
        public BlockBuffer(Func<T[]> fetcher, Action<long> seeker) : this(fetcher, seeker, fetcher()) { }

        /// <summary>
        /// Converts a function that fetches a given chunk of a stream to an object that can fetch a block with any size.
        /// If part of the fetched <see cref="Stream"/> was read, provide it in <paramref name="firstFetch"/>.
        /// </summary>
        public BlockBuffer(Func<T[]> fetcher, Action<long> seeker, T[] firstFetch) {
            Fetcher = fetcher;
            Seeker = seeker;
            LastFetch = firstFetch;
        }

        /// <summary>
        /// Converts a stream reader to a 4 kB block buffer.
        /// </summary>
        public static BlockBuffer<byte> Create(Stream reader) => Create(reader, 4096);

        /// <summary>
        /// Converts a stream reader to a block buffer of fixed size.
        /// </summary>
        public static BlockBuffer<byte> Create(Stream reader, int blockSize) {
            Stream readerCopy = reader;
            int blockSizeCopy = blockSize;
            return new BlockBuffer<byte>(() => reader.ReadBytes(blockSizeCopy), pos => readerCopy.Position = pos);
        }

        /// <summary>
        /// Converts a stream reader to a block buffer of fixed size.
        /// </summary>
        public static BlockBuffer<byte> Create(Stream reader, int blockSize, byte[] firstFetch) {
            Stream readerCopy = reader;
            int blockSizeCopy = blockSize;
            byte[] fetcher() => reader.ReadBytes(blockSizeCopy);
            firstFetch ??= fetcher();
            return new BlockBuffer<byte>(fetcher, pos => readerCopy.Position = pos, firstFetch);
        }

        /// <summary>
        /// For network streams where there is a constant packet size before the client requires a reply, use that packet size, if it's
        /// smaller than the <paramref name="maxBlockSize"/>.
        /// </summary>
        public static BlockBuffer<byte> CreateForConstantPacketSize(Stream reader, int maxBlockSize) =>
            CreateForConstantPacketSize(reader, maxBlockSize, null);

        /// <summary>
        /// For network streams where there is a constant packet size before the client requires a reply, use that packet size, if it's
        /// smaller than the <paramref name="maxBlockSize"/>. If part of the <paramref name="reader"/> was read, provide it in
        /// <paramref name="firstFetch"/>.
        /// </summary>
        public static BlockBuffer<byte> CreateForConstantPacketSize(Stream reader, int maxBlockSize, byte[] firstFetch) {
            int blockSize = maxBlockSize;
            try {
                if (reader is QueueStream queue) {
                    queue.WaitForData(); // If CavernPipe is in use and didn't fill the queue yet, 0 could be passed to blockSize
                }
                if (reader.Length < blockSize) {
                    blockSize = (int)reader.Length;
                    if (firstFetch != null) {
                        blockSize -= firstFetch.Length;
                    }
                }
            } catch {
                // Stream doesn't support the Length property
            }
            return Create(reader, blockSize, firstFetch);
        }

        /// <summary>
        /// Flush the current cache and read a new block. This should be called when a stream position changes.
        /// </summary>
        public void Clear() {
            LastFetch = Fetcher();
            LastFetchStart = 0;
            lastFetchPosition = 0;
        }

        /// <summary>
        /// Read the next fixed number of elements from the stream.
        /// </summary>
        /// <remarks>The returned array can have a smaller length than <paramref name="elements"/>
        /// if there's no more data to be fetched.</remarks>
        public T[] Read(int elements) {
            LastFetchStart = lastFetchPosition;
            if (result.Length != elements) {
                result = new T[elements];
            }
            if (LastFetch == null || LastFetch.Length == 0) {
                return null;
            }
            int pointer = 0;
            while (pointer < elements) {
                int next = Math.Min(elements - pointer, LastFetch.Length - lastFetchPosition);
                for (int i = 0; i < next; ++i) {
                    result[pointer + i] = LastFetch[lastFetchPosition + i];
                }

                pointer += next;
                lastFetchPosition += next;
                if (lastFetchPosition == LastFetch.Length) {
                    LastFetch = Fetcher();
                    if (LastFetch == null || LastFetch.Length == 0) {
                        Array.Clear(result, pointer, result.Length - pointer);
                        return result;
                    }
                    LastFetchStart = 0;
                    lastFetchPosition = 0;
                }
            }
            return result;
        }

        /// <summary>
        /// Read the next value from the stream.
        /// </summary>
        /// <remarks>Returns the default value of <typeparamref name="T"/> when new data can't be fetched.</remarks>
        public T ReadOne() {
            if (lastFetchPosition != LastFetch.Length) {
                LastFetchStart = lastFetchPosition;
                return LastFetch[lastFetchPosition++];
            }
            LastFetch = Fetcher();
            LastFetchStart = 0;
            lastFetchPosition = 1;
            return LastFetch != null ? LastFetch[0] : default;
        }

        /// <summary>
        /// Jumps to a position in the source stream if it's supported.
        /// </summary>
        public void Seek(long position) {
            Seeker(position);
            Clear();
        }
    }
}