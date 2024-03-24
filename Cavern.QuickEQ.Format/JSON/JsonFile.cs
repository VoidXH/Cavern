using System;
using System.Collections.Generic;
using System.Text;

namespace Cavern.Format.JSON {
    /// <summary>
    /// JSON file parser/exporter, representing a single node on the tree.
    /// </summary>
    public sealed class JsonFile {
        /// <summary>
        /// The fields of the current tree node.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, object>> Elements => elements;

        /// <summary>
        /// Index the tree node with the name of a key.
        /// </summary>
        public object this[string key] {
            get {
                for (int i = 0, c = elements.Count; i < c; i++) {
                    if (elements[i].Key == key) {
                        return elements[i].Value;
                    }
                }
                throw new KeyNotFoundException();
            }
        }

        /// <summary>
        /// The fields of the current tree node.
        /// </summary>
        readonly List<KeyValuePair<string, object>> elements;

        /// <summary>
        /// Parse a JSON string to a <see cref="JsonFile"/> instance.
        /// </summary>
        public JsonFile(string contents) {
            int offset = 0;
            elements = Parse(ref contents, ref offset);
        }

        /// <summary>
        /// Create a JSON file from an already existing tree.
        /// </summary>
        internal JsonFile(List<KeyValuePair<string, object>> elements) => this.elements = elements;

        /// <summary>
        /// Parse a single layer of a JSON tree string to a <see cref="JsonFile"/> instance. Parsing of subtrees is recursive.
        /// </summary>
        static List<KeyValuePair<string, object>> Parse(ref string contents, ref int offset) {
            List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();
            string key = null;

            while (offset < contents.Length) {
                switch (contents[offset]) {
                    case '"':
                        offset++;
                        key = ParseString(ref contents, ref offset);
                        break;
                    case ':':
                        offset++;
                        result.Add(new KeyValuePair<string, object>(key, ParseValue(ref contents, ref offset)));
                        break;
                    case '}':
                        return result;
                }

                offset++;
            }

            return result;
        }

        /// <summary>
        /// Parse a string until the closing quotation mark.
        /// </summary>
        static string ParseString(ref string source, ref int offset) {
            StringBuilder result = new StringBuilder();
            while (offset < source.Length) {
                if (source[offset] == '"' && source[offset - 1] != '\\') {
                    break;
                }
                result.Append(source[offset++]);
            }
            return result.ToString();
        }

        /// <summary>
        /// Parse until the next desired character, while skipping them inside strings.
        /// </summary>
        static string ParseUntil(ref string source, ref int offset, params char[] delimiters) {
            StringBuilder result = new StringBuilder();
            while (offset < source.Length) {
                if (source[offset] == '"') {
                    offset++;
                    result.Append('"').Append(ParseString(ref source, ref offset)).Append('"');
                } else if (Array.IndexOf(delimiters, source[offset]) != -1) {
                    return result.ToString();
                } else {
                    result.Append(source[offset]);
                }
                offset++;
            }
            return result.ToString();
        }

        /// <summary>
        /// Parse a value (between the &quot;:&quot; and &quot;,&quot;).
        /// </summary>
        static object ParseValue(ref string source, ref int offset) {
            switch (source[offset]) {
                case '{':
                    return new JsonFile(Parse(ref source, ref offset));
                case '[':
                    offset++;
                    List<object> list = new List<object>();
                    while (offset < source.Length) {
                        list.Add(ParseUntil(ref source, ref offset, ',', ']'));
                        if (source[offset] == ']') {
                            return list.ToArray();
                        } else {
                            offset++;
                        }
                    }
                    return list.ToArray();
                default:
                    return ParseUntil(ref source, ref offset, ',');
            }
        }
    }
}