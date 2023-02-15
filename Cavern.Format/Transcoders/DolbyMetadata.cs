using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    /// <summary>
    /// Transcodes Dolby audio Metadata chunks.
    /// </summary>
    public class DolbyMetadata {
        /// <summary>
        /// Version of this metadata. The bytes are major, minor, revision, and build version numbers.
        /// </summary>
        public uint Version { get; }

        /// <summary>
        /// Channel mode ID, determines the channel layout (acmod).
        /// </summary>
        public int ChannelMode {
            get => programInfo & 0b111;
            set {
                if (value < (1 << 3)) {
                    programInfo = (byte)((programInfo & ~0x7) + value);
                } else {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Bitstream mode ID, determines the content type (bsmod).
        /// </summary>
        public int BitstreamMode {
            get => (programInfo >> 3) & ((1 << 3) - 1);
            set {
                if (value < (1 << 3)) {
                    programInfo = (byte)((programInfo & 0b11000111) + (value << 3));
                } else {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// LFE channel is active (lfeon).
        /// </summary>
        public bool LFE {
            get => (programInfo & (1 << 6)) != 0;
            set => programInfo = (byte)((programInfo & (~(1 << 6))) + (value ? (1 << 6) : 0));
        }

        /// <summary>
        /// Language code is supplied (langcode).
        /// </summary>
        public bool LanguageCodeEnabled {
            get => (dialnormInfo & (1 << 7)) != 0;
            set => dialnormInfo = (byte)((dialnormInfo & (~(1 << 7))) + (value ? (1 << 7) : 0));
        }

        /// <summary>
        /// The content is copyright-protected (copyrightb).
        /// </summary>
        public bool CopyrightBit {
            get => (dialnormInfo & (1 << 6)) != 0;
            set => dialnormInfo = (byte)((dialnormInfo & (~(1 << 6))) + (value ? (1 << 6) : 0));
        }

        /// <summary>
        /// The content is the original bitstream (origbs).
        /// </summary>
        public bool OriginalBitstream {
            get => (dialnormInfo & (1 << 5)) != 0;
            set => dialnormInfo = (byte)((dialnormInfo & (~(1 << 5))) + (value ? (1 << 5) : 0));
        }

        /// <summary>
        /// Apparent loudness of the content (dialnorm).
        /// </summary>
        public int DialogNormalization {
            get => dialnormInfo & ((1 << 5) - 1);
            set {
                if (value < (1 << 5)) {
                    dialnormInfo = (byte)((dialnormInfo & 0b11100000) + value);
                } else {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Software used for creating this DBMD, 2 ASCII strings, 32 characters max.
        /// </summary>
        public string[] CreationInfo { get; } = new string[2];

        /// <summary>
        /// Major/minor/patch versions at the "Created with" field.
        /// </summary>
        public byte[] CreatedWithVersion { get; } = new byte[3];

        /// <summary>
        /// Identifier of content frame rate.
        /// </summary>
        public ushort FrameRateCode { get; set; }

        /// <summary>
        /// Unknown metadata at the beginning of the <see cref="objectMetadata"/> segment.
        /// </summary>
        public uint ObjectMetadataPreamble { get; }

        /// <summary>
        /// Number of audio objects present in the audio stream.
        /// </summary>
        public byte ObjectCount { get; }

        /// <summary>
        /// The program info field of DD+ metadata. Bits:<br />
        /// - 6: <see cref="LFE"/><br />
        /// - 5-3: <see cref="BitstreamMode"/><br />
        /// - 2-0: <see cref="ChannelMode"/>
        /// </summary>
        byte programInfo;

        /// <summary>
        /// The dialog normalization field of DD+ metadata. Bits:<br />
        /// - 7: <see cref="LanguageCodeEnabled"/><br />
        /// - 6: <see cref="CopyrightBit"/><br />
        /// - 5: <see cref="OriginalBitstream"/><br />
        /// - 4-0: <see cref="DialogNormalization"/>
        /// </summary>
        byte dialnormInfo;

        /// <summary>
        /// Downmixing metadata (mode and gains).
        /// </summary>
        readonly ushort downmixInfo;

        /// <summary>
        /// Reads a Dolby audio Metadata chunk from a stream without checking if it's valid or not.
        /// </summary>
        public DolbyMetadata(Stream reader, long length) : this(reader, length, false) { }

        /// <summary>
        /// Reads a Dolby audio Metadata chunk from a stream with an optional validation.
        /// </summary>
        public DolbyMetadata(Stream reader, long length, bool checkChecksums) {
            long endPosition = reader.Position + length;
            Version = reader.ReadUInt32(); // each byte is one dotted value -> to/from string

            byte segmentID;
            byte[] segment = new byte[0];
            while ((segmentID = (byte)reader.ReadByte()) != 0) {
                ushort segmentLength = reader.ReadUInt16();
                if (segment.Length < segmentLength) {
                    segment = new byte[segmentLength];
                }
                int read = reader.Read(segment, 0, segmentLength);
                if (read != segmentLength) {
                    throw new CorruptionException("DBMD length");
                }

                if (checkChecksums) {
                    if (reader.ReadByte() != CalculateChecksum(segment, segmentLength)) {
                        throw new CorruptionException("DBMD segment " + segmentID);
                    }
                } else {
                    ++reader.Position;
                }

                switch (segmentID) {
                    case DolbyDigitalPlusMetadata:
                        programInfo = segment[1];
                        dialnormInfo = segment[5];
                        downmixInfo = (ushort)((segment[8] << 8) + segment[9]);
                        break;
                    case DolbyAtmosMetadata:
                        CreationInfo[0] = segment.ReadCString(0, creationInfoFieldSize);
                        CreationInfo[1] = segment.ReadCString(creationInfoFieldSize, creationInfoFieldSize);
                        for (int i = 0; i < CreatedWithVersion.Length; i++) {
                            CreatedWithVersion[i] = segment[96 + i];
                        }
                        FrameRateCode = (ushort)((segment[111] << 8) + segment[112]);
                        // Find out the following bytes if needed
                        break;
                    case objectMetadata:
                        ObjectMetadataPreamble = segment.ReadUInt32(0);
                        ObjectCount = segment[4];
                        // Find out the following bytes if needed
                        break;
                }
            }
            reader.Position = endPosition;
        }

        /// <summary>
        /// Creates a Dolby Metadata that can be written to a bytestream.
        /// </summary>
        public DolbyMetadata(byte objectCount) {
            Version = version;
            CreationInfo[0] = defaultCreationInfo;
            CreationInfo[1] = Listener.Info[..(Listener.Info.IndexOf('(') - 1)];

            int dot = 0;
            for (int i = 0; i < CreationInfo[1].Length; i++) {
                if (CreationInfo[1][i] >= '0' && CreationInfo[1][i] <= '9') {
                    CreatedWithVersion[dot] = (byte)(CreatedWithVersion[dot] * 10 + CreationInfo[1][i] - '0');
                } else if (CreatedWithVersion[dot] != 0 && ++dot == CreatedWithVersion.Length) {
                    break;
                }
            }

            FrameRateCode = defaultFrameRateCode;
            ObjectMetadataPreamble = objectMetadataPreamble;
            ObjectCount = objectCount;
            programInfo = defaultProgramInfo;
            dialnormInfo = defaultDialnormInfo;
            downmixInfo = defaultDownmixInfo;
        }

        /// <summary>
        /// Gets the checksum value for a metadata segment.
        /// </summary>
        static byte CalculateChecksum(byte[] segment, ushort segmentLength) {
            int checksum = segmentLength;
            for (int i = 0; i < segmentLength; i++) {
                checksum += segment[i];
            }
            return (byte)(~checksum + 1);
        }

        /// <summary>
        /// Create the output bytestream.
        /// </summary>
        public byte[] Serialize() {
            Dictionary<byte, byte[]> segments = new Dictionary<byte, byte[]> {
                { DolbyDigitalPlusMetadata, CreateDolbyDigitalPlusMetadata() }
            };
            if (CreationInfo[0] != null) {
                segments.Add(DolbyAtmosMetadata, CreateDolbyAtmosMetadata());
            }
            if (ObjectCount != 0) {
                segments.Add(objectMetadata, CreateObjectMetadata());
            }

            byte[] result = new byte[6 + 4 * segments.Count + segments.Sum(x => x.Value.Length)];
            result.WriteUInt32(Version, 0);
            int offset = 4;
            foreach (KeyValuePair<byte, byte[]> segment in segments) {
                result[offset++] = segment.Key;
                result.WriteUInt16((ushort)segment.Value.Length, offset);
                Array.Copy(segment.Value, 0, result, offset += 2, segment.Value.Length);
                offset += segment.Value.Length;
                result[offset++] = CalculateChecksum(segment.Value, (ushort)segment.Value.Length);
            }
            return result;
        }

        byte[] CreateDolbyDigitalPlusMetadata() {
            byte[] result = new byte[DolbyDigitalPlusMetadataLength];
            result[1] = programInfo;
            result[5] = dialnormInfo;
            result[8] = (byte)(downmixInfo >> 8);
            result[9] = (byte)downmixInfo;
            return result;
        }

        /// <summary>
        /// Create the bytestream of a <see cref="DolbyAtmosMetadata"/> block.
        /// </summary>
        byte[] CreateDolbyAtmosMetadata() {
            byte[] result = new byte[DolbyAtmosMetadataLength];
            result.WriteCString(CreationInfo[0], 0, creationInfoFieldSize);
            result.WriteCString(CreationInfo[1], creationInfoFieldSize, creationInfoFieldSize);
            for (int i = 0; i < CreatedWithVersion.Length; i++) {
                result[96 + i] = CreatedWithVersion[i];
            }
            result[103] = 0x03; // no idea
            result[106] = 0x01; // no idea
            result[111] = (byte)(FrameRateCode >> 8);
            result[112] = (byte)FrameRateCode;
            return result;
        }

        /// <summary>
        /// Create the bytestream of an <see cref="objectMetadata"/> block.
        /// </summary>
        byte[] CreateObjectMetadata() {
            byte[] result = new byte[5 + objectMetadataTrashLength + ObjectCount];
            result.WriteUInt32(ObjectMetadataPreamble, 0);
            result[4] = ObjectCount;
            for (int i = result.Length - ObjectCount; i < result.Length; i++) {
                result[i] = defaultObjectMetadata;
            }
            return result;
        }

        /// <summary>
        /// Version used for writing DBMDs.
        /// </summary>
        const uint version = 0x01000006;

        /// <summary>
        /// Dolby Digital Plus metadata segment identifier.
        /// </summary>
        const byte DolbyDigitalPlusMetadata = 7;

        /// <summary>
        /// Default length of a <see cref="DolbyDigitalPlusMetadata"/> segment.
        /// </summary>
        const ushort DolbyDigitalPlusMetadataLength = 96;

        /// <summary>
        /// Default to 5.1 layout.
        /// </summary>
        const byte defaultProgramInfo = 0x47;

        /// <summary>
        /// If no information is provided about them, protection should be enabled and the content is marked as original.
        /// </summary>
        const byte defaultDialnormInfo = 0x60;

        /// <summary>
        /// Default downmix is -3 dB on each channel.
        /// </summary>
        const ushort defaultDownmixInfo = 0x2424;

        /// <summary>
        /// Dolby Atmos metadata segment identifier.
        /// </summary>
        const byte DolbyAtmosMetadata = 9;

        /// <summary>
        /// Default length of a <see cref="DolbyAtmosMetadata"/> segment.
        /// </summary>
        const ushort DolbyAtmosMetadataLength = 248;

        /// <summary>
        /// Creation info used for writing DBMDs. This default value has to stay for the DMBD to be valid.
        /// </summary>
        const string defaultCreationInfo = "Created using Dolby equipment";

        /// <summary>
        /// Maximum number of characters for each creation info entry.
        /// </summary>
        const byte creationInfoFieldSize = 32;

        /// <summary>
        /// Default value of <see cref="FrameRateCode"/>.
        /// </summary>
        const ushort defaultFrameRateCode = 0x22FF;

        /// <summary>
        /// Object-related metadata.
        /// </summary>
        const byte objectMetadata = 10;

        /// <summary>
        /// Unknown values that were the same for every checked <see cref="objectMetadata"/> segment.
        /// </summary>
        const uint objectMetadataPreamble = 0xf8726fbd;

        /// <summary>
        /// Fixed length of skipped fields in <see cref="objectMetadata"/>.
        /// </summary>
        const ushort objectMetadataTrashLength = 262;

        /// <summary>
        /// Default value of a single object's metadata.
        /// </summary>
        const byte defaultObjectMetadata = 0x84;
    }
}