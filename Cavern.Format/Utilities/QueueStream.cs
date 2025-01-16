using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// A thread-safe FIFO <see cref="MemoryStream"/>.
    /// </summary>
    public class QueueStream : Stream {
        /// <summary>
        /// When data is available, lets the threads through, otherwise waits until the data arrives.
        /// </summary>
        readonly ManualResetEventSlim dataAvailable = new ManualResetEventSlim(false);

        /// <summary>
        /// The underlying FIFO.
        /// </summary>
        Queue<byte> queue = new Queue<byte>();

        /// <inheritdoc/>
        public override bool CanRead {
            get {
                lock (queue) {
                    return queue.Count != 0;
                }
            }
        }

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length {
            get {
                lock (queue) {
                    return queue.Count;
                }
            }
        }

        /// <inheritdoc/>
        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) {
            int read = 0;
            while (read < count) {
                lock (queue) {
                    int readUntil = Math.Min(read + queue.Count, count);
                    for (; read < readUntil; read++) {
                        buffer[offset + read] = queue.Dequeue();
                    }
                }
            }

            lock (queue) {
                if (queue.Count == 0) {
                    dataAvailable.Reset();
                }
            }
            return count;
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) {
            lock (queue) {
                for (int i = 0; i < count; i++) {
                    queue.Enqueue(buffer[offset + i]);
                }
            }
            dataAvailable.Set();
        }

        /// <summary>
        /// If the <see cref="queue"/> is empty, waits for a bulk <see cref="Write"/> so any complete data becomes available.
        /// </summary>
        public void WaitForData() => dataAvailable.Wait();

        /// <inheritdoc/>
        public override void Flush() => queue.Clear();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            queue = null;
            base.Dispose(disposing);
        }
    }
}