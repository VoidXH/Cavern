using System.Collections.Generic;
using System.IO;

using Cavern.Format.Common;

namespace Cavern.Format.Container.Matroska {
    /// <summary>
    /// Builds a tree of a Matroska file's neccessary tags.
    /// </summary>
    /// <see href="https://github.com/ietf-wg-cellar/matroska-specification/blob/master/ebml_matroska.xml"/>
    public partial class MatroskaTree : KeyLengthValue {
        /// <summary>
        /// Last byte (exclusive) of the file that is a tag in this element.
        /// </summary>
        protected readonly long end;

        /// <summary>
        /// The contained subtree.
        /// </summary>
        readonly List<MatroskaTree> children = new List<MatroskaTree>();

        /// <summary>
        /// Location in the file where the next child should be read from.
        /// </summary>
        long nextTag;

        /// <summary>
        /// Cache for <see cref="GetChild(Stream, int, int)"/>, contains which the indices of
        /// already read <see cref="children"/> are for a given tag (key).
        /// </summary>
        Dictionary<int, List<int>> childIndices;

        /// <summary>
        /// Build the next KLV subtree.
        /// </summary>
        public MatroskaTree(Stream reader) : base(reader) {
            nextTag = reader.Position;
            end = nextTag + Length;
            reader.Position = end;
        }

        /// <summary>
        /// Build the next KLV subtree while checking if it's in range of the file (<paramref name="valid"/>) or not.
        /// </summary>
        MatroskaTree(Stream reader, long endPosition, out bool valid) : base(reader) {
            nextTag = reader.Position;
            end = nextTag + Length;
            valid = end < endPosition;
            if (valid) {
                reader.Position = end;
            }
        }

        /// <summary>
        /// Parses a tree item if possible, returns null if not.
        /// </summary>
        /// <param name="reader">Matroska stream to read from</param>
        /// <param name="endPosition">Location of the final byte in the stream (exclusive).</param>
        public static MatroskaTree TryCreate(Stream reader, long endPosition) {
            MatroskaTree result = new MatroskaTree(reader, endPosition, out bool valid);
            return valid ? result : null;
        }

        /// <summary>
        /// Fetch the first child of a tag if it exists.
        /// </summary>
        public MatroskaTree GetChild(Stream reader, int tag) {
            for (int i = 0, c = children.Count; i < c; ++i) {
                if (children[i].Tag == tag) {
                    return children[i];
                }
            }

            reader.Position = nextTag;
            while (nextTag < end) {
                MatroskaTree subtree = new MatroskaTree(reader);
                children.Add(subtree);
                if (subtree.Tag == tag) {
                    return subtree;
                }
                nextTag = reader.Position;
            }
            return null;
        }

        /// <summary>
        /// Get a specific child by its order of the same kind of children.
        /// </summary>
        public MatroskaTree GetChild(Stream reader, int tag, int index) {
            childIndices ??= new Dictionary<int, List<int>>();
            List<int> indices;
            if (childIndices.ContainsKey(tag)) {
                indices = childIndices[tag];
            } else {
                indices = childIndices[tag] = new List<int>();
            }

            int c = indices.Count;
            if (index < indices.Count) {
                return children[indices[index]];
            }

            int lastChild = 0;
            if (c != 0) {
                lastChild = indices[c - 1] + 1;
            }
            for (int i = lastChild, childCount = children.Count; i < childCount; ++i) {
                if (children[i].Tag == tag) {
                    indices.Add(i);
                    if (c++ == index) {
                        return children[i];
                    }
                }
            }

            int tagIndex = children.Count;
            reader.Position = nextTag;
            while (nextTag < end) {
                MatroskaTree subtree = TryCreate(reader, end);
                if (subtree == null) {
                    nextTag = end;
                    return null;
                }

                children.Add(subtree);
                if (subtree.Tag == tag) {
                    indices.Add(tagIndex);
                    if (c++ == index) {
                        nextTag = reader.Position;
                        return subtree;
                    }
                }
                ++tagIndex;
            }
            nextTag = reader.Position;
            return null;
        }

        /// <summary>
        /// Fetch all child instances.
        /// </summary>
        public MatroskaTree[] GetChildren(Stream reader) {
            ReadAllChildren(reader);
            return children.ToArray();
        }

        /// <summary>
        /// Fetch all child instances of a <paramref name="tag"/>.
        /// </summary>
        public MatroskaTree[] GetChildren(Stream reader, int tag) {
            ReadAllChildren(reader);
            int tags = 0;
            for (int i = 0, c = children.Count; i < c; i++) {
                if (children[i].Tag == tag) {
                    tags++;
                }
            }

            MatroskaTree[] result = new MatroskaTree[tags];
            for (int i = 0, c = children.Count; i < c; i++) {
                if (children[i].Tag == tag) {
                    result[^tags] = children[i];
                    tags--;
                }
            }
            return result;
        }

        /// <summary>
        /// Get the first found child's big-endian floating point value by tag if it exists.
        /// </summary>
        public double GetChildFloatBE(Stream reader, int tag) {
            MatroskaTree child = GetChild(reader, tag);
            return child != null ? child.GetFloatBE(reader) : -1;
        }

        /// <summary>
        /// Get the first found child's UTF-8 value by tag if it exists.
        /// </summary>
        public string GetChildUTF8(Stream reader, int tag) {
            MatroskaTree child = GetChild(reader, tag);
            return child != null ? child.GetUTF8(reader) : string.Empty;
        }

        /// <summary>
        /// Get the first found child's <see cref="VarInt"/> value by tag if it exists.
        /// </summary>
        public long GetChildValue(Stream reader, int tag) {
            MatroskaTree child = GetChild(reader, tag);
            return child != null ? child.GetValue(reader) : -1;
        }

        /// <summary>
        /// Get the index of a child for <see cref="GetChild(Stream, int, int)"/> by its position in the file stream.
        /// </summary>
        /// <remarks>This <paramref name="position"/> is not the same as <see cref="KeyLengthValue.position"/>, that
        /// has to be matched first by reading the metadata for the element.</remarks>
        public int GetIndexByPosition(Stream reader, int tag, long position) {
            reader.Position = position;
            MatroskaTree element = new MatroskaTree(reader);
            position = element.position;

            int result = 0;
            while (true) {
                MatroskaTree child = GetChild(reader, tag, result);
                if (child == null) {
                    return -1;
                }
                if (child.position == position) {
                    return result;
                }
                ++result;
            }
        }

        /// <summary>
        /// Read the remainder of the value of this element to a byte array.
        /// </summary>
        public byte[] GetRawData(Stream reader) {
            reader.Position = nextTag;
            byte[] result = new byte[end - nextTag];
            int read = reader.Read(result);
            if (read != result.Length) {
                throw new EndOfStreamException();
            }
            return result;
        }

        /// <summary>
        /// Make sure all children are parsed to the <see cref="children"/> list.
        /// </summary>
        void ReadAllChildren(Stream reader) {
            reader.Position = nextTag;
            while (reader.Position < end) {
                MatroskaTree subtree = new MatroskaTree(reader);
                children.Add(subtree);
            }
            nextTag = end;
        }

        /// <summary>
        /// Display the tag in HEX when converting to string.
        /// </summary>
        public override string ToString() => "0x" + Tag.ToString("X8");
    }
}
