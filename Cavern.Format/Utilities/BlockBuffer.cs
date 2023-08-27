using System;
using System.IO;

using Cavern.Format.Common;

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
        public BlockBuffer(Func<T[]> fetcher) {
            Fetcher = fetcher;
            LastFetch = Fetcher();
        }

        /// <summary>
        /// Converts a function that fetches a given chunk of a stream to an object that can fetch a block with any size.
        /// </summary>
        public BlockBuffer(Func<T[]> fetcher, Action<long> seeker) {
            Fetcher = fetcher;
            Seeker = seeker;
            LastFetch = Fetcher();
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