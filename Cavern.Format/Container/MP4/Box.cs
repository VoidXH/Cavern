﻿using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Architectural data block of the ISO-BMFF format, including the MP4 container.
    /// </summary>
    internal class Box {
        /// <summary>
        /// Bytes of data contained in this box.
        /// </summary>
        public uint Length { get; }

        /// <summary>
        /// 4 character descriptor of the box's contents.
        /// </summary>
        public uint Header { get; }

        /// <summary>
        /// Data starts from this offset in the input stream.
        /// </summary>
        protected readonly long position;

        /// <summary>
        /// Stores the metadata of an ISO-BMFF box.
        /// </summary>
        protected Box(uint length, uint header, Stream reader) {
            Length = length;
            Header = header;
            position = reader.Position;
        }

        /// <summary>
        /// Stores the metadata of an ISO-BMFF box.
        /// </summary>
        Box(uint length, uint header, long position) {
            Length = length;
            Header = header;
            this.position = position;
        }

        /// <summary>
        /// Return a box in an object for its type.
        /// </summary>
        public static Box Parse(Stream reader) {
            uint length = reader.ReadUInt32BE() - 8;
            uint header = reader.ReadUInt32BE();
            long nextBox = reader.Position + length;

            if (header == freeBox) {
                reader.Position = nextBox;
                return Parse(reader);
            }

            Box result = header switch {
                fileTypeBox => new FileTypeBox(length, reader),
                rawBox => new RawBox(length, reader),
                metadataBox => new NestedBox(length, header, reader),
                trackBox => new TrackBox(length, reader),
                mediaBox => new NestedBox(length, header, reader),
                mediaInfoBox => new NestedBox(length, header, reader),
                sampleTableBox => new NestedBox(length, header, reader),
                sampleDescriptionBox => new SampleDescriptionBox(length, reader),
                timeToSampleBox => new TimeToSampleBox(length, reader),
                sampleToChunkBox => new SampleToChunkBox(length, reader),
                _ => new Box(length, header, reader.Position)
            };
            reader.Position = nextBox;
            return result;
        }

        /// <summary>
        /// Get the bytes contained in the box.
        /// </summary>
        public byte[] GetRawData(Stream reader) {
            reader.Position = position;
            byte[] result = new byte[Length];
            reader.Read(result);
            return result;
        }

        /// <summary>
        /// Box metadata to string.
        /// </summary>
        public override string ToString() => $"{Header.ToFourCC()} ({Length} bytes)";

        /// <summary>
        /// Throw an exception that a required child box was not found.
        /// </summary>
        protected internal void ThrowCorruption(uint box) =>
            throw new CorruptionException(string.Format(corruptionMessage, box, position));

        /// <summary>
        /// Message sent with the exception when a required box was not found.
        /// </summary>
        const string corruptionMessage = "{0} was not found after {1}";
    }
}