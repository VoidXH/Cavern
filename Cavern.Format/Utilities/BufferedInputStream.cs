﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Constantly reads a stream in the background, and keeps a window in memory around the last read position for fast data access.
    /// </summary>
    public class BufferedInputStream : Stream, IDisposable {
        /// <summary>
        /// Size of each cached block in bytes. The file is read in this big chunks.
        /// </summary>
        public int BlockSize {
            get => blockSize;
            set {
                blockSize = value;
                offset = Math.Max((position / blockSize - (BlockCount >> 1)) * blockSize, 0);
                lock (blockLock) {
                    Array.Clear(blocks, 0, blocks.Length);
                }
                update.Set();
            }
        }

        /// <summary>
        /// Maximum number of blocks to cache, adding together forward and backward caches.
        /// The total cache size is is <see cref="BlockCount"/> * <see cref="BlockSize"/>.
        /// </summary>
        public int BlockCount {
            get => blocks.Length;
            set {
                lock (blockLock) {
                    Array.Resize(ref blocks, value);
                }
                update.Set();
            }
        }

        /// <summary>
        /// The wrapped stream can be read.
        /// </summary>
        public override bool CanRead => stream.CanRead;

        /// <summary>
        /// The wrapped stream supports seeking.
        /// </summary>
        public override bool CanSeek => stream.CanSeek;

        /// <summary>
        /// Input streams cannot be written.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Length of the wapped stream.
        /// </summary>
        public override long Length => stream.Length;

        /// <summary>
        /// Window center position in the wrapped stream.
        /// </summary>
        public override long Position {
            get => position;
            set => Seek(value, SeekOrigin.Begin);
        }

        /// <summary>
        /// Size of each cached block. The file is read in this big chunks.
        /// </summary>
        int blockSize;

        /// <summary>
        /// Window center position in the wrapped stream.
        /// </summary>
        long position;

        /// <summary>
        /// The location of <see cref="blocks"/>[0] in the file.
        /// </summary>
        long offset;

        /// <summary>
        /// Blocks cached in memory.
        /// </summary>
        byte[][] blocks = new byte[0][];

        /// <summary>
        /// Used as a lock for <see cref="blocks"/>.
        /// </summary>
        readonly object blockLock = new object();

        /// <summary>
        /// A read is in progress. Seeking is blocked until it's finished.
        /// </summary>
        readonly object readLock = new object();

        /// <summary>
        /// The <see cref="blocks"/> array was modified, null elements shall be cached.
        /// </summary>
        readonly AutoResetEvent update = new AutoResetEvent(true);

        /// <summary>
        /// The wrapped stream.
        /// </summary>
        readonly Stream stream;

        /// <summary>
        /// Runs the <see cref="Worker"/>.
        /// </summary>
        readonly Task runner;

        /// <summary>
        /// Wrap a stream to constantly read it in the background, and keep a window in memory
        /// around the last read position for fast data access.
        /// </summary>
        public BufferedInputStream(Stream stream, int defaultBlockSize, int defaultBlockCount) {
            this.stream = stream;
            blockSize = defaultBlockSize;
            BlockCount = defaultBlockCount;
            runner = new Task(Worker);
            runner.Start();
        }

        /// <summary>
        /// Clears all buffers for the wrapped stream.
        /// </summary>
        public override void Flush() => stream.Flush();

        /// <summary>
        /// Read to a buffer from the wrapped stream. This is thread-safe.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count) {
            int done = 0;
            lock (readLock) {
                while (done < count) {
                    BlockUntilReadable();
                    lock (blockLock) {
                        byte[] block = blocks[(position - this.offset) / blockSize];
                        int blockOffset = (int)(position % blockSize),
                            readUntil = Math.Min(blockSize, blockOffset + count - done);
                        for (int i = blockOffset; i < readUntil; i++) {
                            buffer[offset++] = block[i];
                        }

                        position += readUntil - blockOffset;
                        done += readUntil - blockOffset;
                        MoveBlocksIfNeeded();
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Read a single byte from the wrapped stream. This is thread-safe.
        /// </summary>
        public override int ReadByte() {
#if DEBUG
            if (position < offset) {
                throw new IndexOutOfRangeException();
            }
#endif
            lock (readLock) {
                BlockUntilReadable();
                lock (blockLock) {
                    int block = (int)((position - offset) / blockSize);
                    byte result = blocks[block][position++ % blockSize];
                    MoveBlocksIfNeeded();
                    return result;
                }
            }
        }

        /// <summary>
        /// Move the reading pointer to a specific offset in the stream and the cache. This is thread-safe.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin) {
            lock (readLock) {
                lock (blockLock) {
                    long oldAlignment = position - position % blockSize;
                    if (origin == SeekOrigin.Begin) {
                        position = offset;
                    } else if (origin == SeekOrigin.Current) {
                        position += offset;
                    } else {
                        position = Length + offset;
                    }
                    stream.Position = position;

                    if (this.offset < position && position - this.offset < (blocks.Length >> 1) * blockSize) {
                        return position;
                    }

                    int moveBlocksBy = -(int)((position - position % blockSize - oldAlignment) / blockSize);
                    this.offset -= moveBlocksBy * blockSize;
                    int pointerOffsetFromCenter = (int)((position - offset) / blockSize) - (blocks.Length >> 1);
                    if (pointerOffsetFromCenter > 0) {
                        moveBlocksBy -= pointerOffsetFromCenter;
                        this.offset += pointerOffsetFromCenter * blockSize;
                    }

                    if (moveBlocksBy != 0) {
                        if (moveBlocksBy < 0) {
                            if (blocks.Length > -moveBlocksBy) {
                                Array.Copy(blocks, -moveBlocksBy, blocks, 0, blocks.Length + moveBlocksBy);
                                Array.Clear(blocks, blocks.Length + moveBlocksBy, -moveBlocksBy);
                            } else {
                                Array.Clear(blocks, 0, blocks.Length);
                            }
                        } else {
                            if (blocks.Length > moveBlocksBy) {
                                Array.Copy(blocks, 0, blocks, moveBlocksBy, blocks.Length - moveBlocksBy);
                                Array.Clear(blocks, 0, moveBlocksBy);
                            } else {
                                Array.Clear(blocks, 0, blocks.Length);
                            }
                        }
                        update.Set();
                    }
                }
            }
            return position;
        }

        /// <summary>
        /// Input streams' length cannot be changed.
        /// </summary>
        public override void SetLength(long value) => throw new InvalidOperationException();

        /// <summary>
        /// Input streams cannot be written.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException();

        /// <summary>
        /// Releases the unmanaged resources used by the System.IO.Stream and optionally releases the managed resources.
        /// </summary>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            lock (blockLock) {
                blocks = null;
            }
            update.Set();
            runner.Wait();
            runner.Dispose();
            stream.Dispose();
        }

        /// <summary>
        /// Block execution until the file <see cref="blocks"/> that is at the current <see cref="position"/> can be read.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void BlockUntilReadable() {
            while (position - offset >= blocks.Length * blockSize) {
                // Block while the current block is not reached
            }
            while (blocks[(position - offset) / blockSize] == null) {
                // Block while the current block is under reading
            }
        }

        /// <summary>
        /// If the position passed the center of the cache, start caching.
        /// </summary>
        void MoveBlocksIfNeeded() {
            int pointerOffsetFromCenter = (int)((position - offset) / blockSize) - (blocks.Length >> 1);
            if (pointerOffsetFromCenter > 0) {
                offset += pointerOffsetFromCenter * blockSize;
                if (pointerOffsetFromCenter < blocks.Length) {
                    Array.Copy(blocks, pointerOffsetFromCenter, blocks, 0, blocks.Length - pointerOffsetFromCenter);
                    Array.Clear(blocks, blocks.Length - pointerOffsetFromCenter, pointerOffsetFromCenter);
                } else {
                    Array.Clear(blocks, 0, blocks.Length);
                }
                update.Set();
            }
        }

        /// <summary>
        /// Constant stream cacher function.
        /// </summary>
        void Worker() {
            while (blocks != null) {
                update.WaitOne();

                while (blocks != null) {
                    // Find a target block: start reading ahead from the position, and if that's done, read backwards
                    long readBlockFrom;
                    lock (blockLock) {
                        int currentBlock = (int)((position - offset) / blockSize);
                        int targetBlock = -1;
                        for (int i = currentBlock; i < blocks.Length; i++) {
                            if (blocks[i] == null) {
                                targetBlock = i;
                                break;
                            }
                        }
                        if (targetBlock == -1) {
                            for (int i = currentBlock - 1; i >= 0; i--) {
                                if (blocks[i] == null) {
                                    targetBlock = i;
                                    break;
                                }
                            }
                        }

                        if (targetBlock == -1) {
                            break;
                        }
                        readBlockFrom = offset + targetBlock * (long)blockSize;
                    }

                    // Read the target block
                    stream.Position = readBlockFrom;
                    byte[] newBlock = new byte[blockSize];
                    stream.Read(newBlock, 0, blockSize);

                    // Put the target block in the buffer, and handle if it has moved
                    lock (blockLock) {
                        int index = (int)((readBlockFrom - offset) / blockSize);
                        if (blocks != null && index >= 0 && index < blocks.Length) {
                            blocks[index] = newBlock;
                        }
                    }
                }
            }
        }
    }
}