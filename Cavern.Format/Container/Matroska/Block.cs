using System.IO;

using Cavern.Format.Common;

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
        /// Used <see cref="Track"/> from <see cref="MatroskaReader.Tracks"/>.
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
        readonly byte frameCount;

        /// <summary>
        /// Length of each frame's raw data.
        /// </summary>
        readonly int[] frameSizes;

        /// <summary>
        /// Position of the first byte of the first frame in the file.
        /// </summary>
        readonly long firstFrame;

        /// <summary>
        /// Parse the metadata of a data block.
        /// </summary>
        public Block(BinaryReader reader, MatroskaTree source) {
            source.Position(reader);
            long start = reader.BaseStream.Position;
            Track = VarInt.ReadValue(reader);
            TimeStamp = reader.ReadInt16();
            flags = reader.ReadByte();

            Lacing lacing = LacingType;
            if (lacing != Lacing.None) {
                frameCount = (byte)(reader.ReadByte() + 1);
                frameSizes = new int[frameCount];
                firstFrame = reader.BaseStream.Position;
                switch (lacing) {
                    case Lacing.Xiph:
                        byte frame = 0;
                        while (frame < frameCount) {
                            byte value;
                            int sum = 0;
                            do {
                                value = reader.ReadByte();
                                sum += value;
                            } while (value == 255);
                            frameSizes[frame++] = sum;
                        }
                        break;
                    case Lacing.FixedSize:
                        int step = (int)((source.Length - (firstFrame - start)) / frameCount);
                        for (byte i = 0; i < frameCount; ++i)
                            frameSizes[i] = step;
                        break;
                    case Lacing.EBML:
                        frameSizes[0] = (int)VarInt.ReadValue(reader);
                        for (byte i = 1; i < frameCount; ++i)
                            frameSizes[i] = (int)VarInt.ReadSignedValue(reader);
                        for (byte i = (byte)(frameCount - 1); i > 0; --i)
                            frameSizes[i] -= frameSizes[i - 1];
                        break;
                    default:
                        break;
                }
            } else {
                firstFrame = reader.BaseStream.Position;
                frameCount = 1;
                frameSizes = new int[1] { (int)(source.Length - (firstFrame - start)) };
            }
        }

        /// <summary>
        /// Read all stream data from this block, without separating frames.
        /// </summary>
        public byte[] GetData(BinaryReader reader) {
            reader.BaseStream.Position = firstFrame;
            int length = Utilities.QMath.Sum(frameSizes);
            return reader.ReadBytes(length);
        }

        /// <summary>
        /// Get the raw stream bytes for each frame contained in this block.
        /// </summary>
        public byte[][] GetFrames(BinaryReader reader) {
            reader.BaseStream.Position = firstFrame;
            byte[][] result = new byte[frameCount][];
            for (byte frame = 0; frame < frameCount; ++frame)
                result[frame] = reader.ReadBytes(frameSizes[frame]);
            return result;
        }
    }
}