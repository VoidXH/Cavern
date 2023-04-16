using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Container.Matroska {
    /// <summary>
    /// One or multiple frames of a Matroska file's single track.
    /// </summary>
    internal class Block {
        public enum Lacing {
            None = 0x00,
            Xiph = 0x02,
            FixedSize = 0x04,
            EBML = 0x06
        }

        /// <summary>
        /// The frame itself doesn't reference any other frames and no frame after this can reference any frame before this.
        /// </summary>
        public bool IsKeyframe => (flags & keyframeFlag) != 0;

        /// <summary>
        /// The duration of this block is 0.
        /// </summary>
        public bool IsInvisible => (flags & invisibleFlag) != 0;

        /// <summary>
        /// Used method of storing multiple frames in this block.
        /// </summary>
        public Lacing LacingType => (Lacing)(flags & lacingFlags);

        /// <summary>
        /// This frame can be dropped on poor decoder performance.
        /// </summary>
        public bool IsDiscardable => (flags & discardableFlag) != 0;

        /// <summary>
        /// Used <see cref="Track"/> from a <see cref="MatroskaReader"/>.
        /// </summary>
        public long Track { get; private set; }

        /// <summary>
        /// Timing relative to <see cref="Cluster.TimeStamp"/>.
        /// </summary>
        public short TimeStamp { get; private set; }

        /// <summary>
        /// Bit mask of property flags.
        /// </summary>
        readonly byte flags;

        /// <summary>
        /// Number of frames contained in this block.
        /// </summary>
        readonly int frameCount;

        /// <summary>
        /// Length of each frame's raw data.
        /// </summary>
        readonly int[] frameSizes;

        /// <summary>
        /// Position of the first byte of the first frame in the file.
        /// </summary>
        readonly long firstFrame;

        /// <summary>
        /// Source stream of the block, which might be a <see cref="MemoryStream"/>.
        /// </summary>
        readonly Stream reader;

        /// <summary>
        /// Parse the metadata of a data block.
        /// </summary>
        public Block(Stream reader, MatroskaTree source) {
            source.Position(reader);
            this.reader = reader;
            long start = reader.Position;
            Track = VarInt.ReadValue(reader);
            TimeStamp = reader.ReadInt16BE();
            flags = (byte)reader.ReadByte();

            Lacing lacing = LacingType;
            if (lacing != Lacing.None) {
                int x = reader.ReadByte();
                frameCount = x + 1;
                frameSizes = new int[frameCount];
                firstFrame = reader.Position;
                switch (lacing) {
                    case Lacing.Xiph:
                        byte frame = 0;
                        long totalSize = 0;
                        while (frame < frameCount - 1) {
                            byte value;
                            int sum = 0;
                            do {
                                value = (byte)reader.ReadByte();
                                sum += value;
                            } while (value == 255);
                            totalSize += sum;
                            frameSizes[frame++] = sum;
                        }
                        frameSizes[frame] = (int)(source.Length - (reader.Position - start) - totalSize);
                        break;
                    case Lacing.FixedSize:
                        int step = (int)((source.Length - (firstFrame - start)) / frameCount);
                        for (byte i = 0; i < frameCount; ++i) {
                            frameSizes[i] = step;
                        }
                        break;
                    case Lacing.EBML:
                        frameSizes[0] = (int)VarInt.ReadValue(reader);
                        long last = -frameSizes[0];
                        for (int i = 1; i < frameCount - 1; ++i) {
                            frameSizes[i] = frameSizes[i - 1] + (int)VarInt.ReadSignedValue(reader);
                            last -= frameSizes[i];
                        }
                        frameSizes[frameCount - 1] = (int)(last + source.Length - (reader.Position - start));
                        break;
                }
            } else {
                firstFrame = reader.Position;
                frameCount = 1;
                frameSizes = new[] { (int)(source.Length - (firstFrame - start)) };
            }
        }

        /// <summary>
        /// Write a block of data to a Matroska file that's under creation.
        /// </summary>
        /// <param name="tree">EBML writer object</param>
        /// <param name="writer">The stream used by the <paramref name="tree"/> writer for writing raw bytes</param>
        /// <param name="keyframe">This block can be decoded on its own</param>
        /// <param name="track">Unique identifier of the track</param>
        /// <param name="timeStamp">Timing relative to <see cref="Cluster.TimeStamp"/></param>
        /// <param name="data">Raw data of the block</param>
        /// <remarks>Only a single frame can be written using this function.</remarks>
        public static void Write(MatroskaTreeWriter tree, Stream writer, bool keyframe, int track, short timeStamp, byte[] data) {
            tree.OpenSequence(MatroskaTree.Segment_Cluster_SimpleBlock, 3);
            VarInt.Write(writer, track);
            writer.WriteByte((byte)(timeStamp >> 8));
            writer.WriteByte((byte)timeStamp);
            writer.WriteByte(keyframe ? keyframeFlag : (byte)0);
            writer.Write(data);
            tree.CloseSequence();
        }

        /// <summary>
        /// Read all stream data from this block, without separating frames.
        /// </summary>
        public byte[] GetData() {
            reader.Position = firstFrame;
            return reader.ReadBytes(frameSizes.Sum());
        }

        /// <summary>
        /// Get the raw stream bytes for each frame contained in this block.
        /// </summary>
        public byte[][] GetFrames() {
            reader.Position = firstFrame;
            byte[][] result = new byte[frameCount][];
            for (byte frame = 0; frame < frameCount; ++frame) {
                result[frame] = reader.ReadBytes(frameSizes[frame]);
            }
            return result;
        }

        /// <summary>
        /// Provides basic information about the block.
        /// </summary>
        public override string ToString() => $"Matroska block, track {Track}, relative time: {TimeStamp}";

        /// <summary>
        /// Flag mask for <see cref="IsKeyframe"/>.
        /// </summary>
        const byte keyframeFlag = 0x80;

        /// <summary>
        /// Flag mask for <see cref="IsInvisible"/>.
        /// </summary>
        const byte invisibleFlag = 0x08;

        /// <summary>
        /// Flag mask for <see cref="LacingType"/>.
        /// </summary>
        const byte lacingFlags = 0x06;

        /// <summary>
        /// Flag mask for <see cref="IsDiscardable"/>.
        /// </summary>
        const byte discardableFlag = 0x01;
    }
}