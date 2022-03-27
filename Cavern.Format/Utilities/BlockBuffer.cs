using System;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Converts a function that fetches a given chunk of a stream to an object that can fetch a block with any size.
    /// </summary>
    public sealed class BlockBuffer<T> {
        /// <summary>
        /// Result of the last <see cref="Fetcher"/> call.
        /// </summary>
        T[] lastFetch;

        /// <summary>
        /// First sample from <see cref="lastFetch"/> that wasn't collected.
        /// </summary>
        int lastFetchPosition;

        /// <summary>
        /// Calls when new block data is needed.
        /// </summary>
        event Func<T[]> Fetcher;

        /// <summary>
        /// Converts a function that fetches a given chunk of a stream to an object that can fetch a block with any size.
        /// </summary>
        public BlockBuffer(Func<T[]> fetcher) {
            Fetcher = fetcher;
            lastFetch = new T[0];
        }

        /// <summary>
        /// Read the next fixed number of elements from the stream.
        /// </summary>
        /// <remarks>The returned array can have a smaller length than <paramref name="elements"/>
        /// if there's no more data to be fetched.</remarks>
        public T[] Read(int elements) {
            T[] result = new T[elements];
            int pointer = 0;
            while (pointer < elements) {
                int next = Math.Min(elements - pointer, lastFetch.Length - lastFetchPosition);
                for (int i = 0; i < next; ++i)
                    result[pointer + i] = lastFetch[lastFetchPosition + i];

                pointer += next;
                lastFetchPosition += next;
                if (lastFetchPosition == lastFetch.Length) {
                    lastFetch = Fetcher();
                    if (lastFetch == null) {
                        Array.Resize(ref result, pointer);
                        return result;
                    }
                    lastFetchPosition = 0;
                }
            }
            return result;
        }
    }
}