using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using Cavern.Format.Utilities;

namespace Cavern.Format.JSON {
    /// <summary>
    /// JSON file parser/exporter, representing a single node on the tree.
    /// For example usage, check the code of <see cref="FilterSet.JLAudioTuNFilterSet"/>.
    /// </summary>
    public sealed class JsonFile : IEnumerable<KeyValuePair<string, object>> {
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
            set {
                for (int i = 0, c = elements.Count; i < c; i++) {
                    if (elements[i].Key == key) {
                        elements[i] = key.Stores(value);
                    }
                }
                elements.Add(key.Stores(value));
            }
        }

        /// <summary>
        /// The fields of the current tree node.
        /// </summary>
        readonly List<KeyValuePair<string, object>> elements;

        /// <summary>
        /// Create an empty JSON file tree node.
        /// </summary>
        public JsonFile() => elements = new List<KeyValuePair<string, object>>();

        /// <summary>
        /// Parse a JSON string to a <see cref="JsonFile"/> instance.
        /// </summary>
        public JsonFile(string contents) {
            int offset = 0;
            elements = Parse(ref contents, ref offset);
        }

        /// <summary>
        /// Create a JSON file with a single element.
        /// </summary>
        public JsonFile(string key, object value) => elements = new List<KeyValuePair<string, object>> {
            key.Stores(value)
        };

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
                        result.Add(key.Stores(ParseValue(ref contents, ref offset)));
                        break;
                    case '}':
                        offset++;
                        return result;
                }

                if (contents[offset] != '}') {
                    offset++; // Parsing stops at end of items, arrays, objects
                }
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
            SkipWhitespace(ref source, ref offset);
            switch (source[offset]) {
                case '{':
                    return new JsonFile(Parse(ref source, ref offset));
                case '[':
                    offset++;
                    SkipWhitespace(ref source, ref offset);
                    List<object> list = new List<object>();
                    while (offset < source.Length) {
                        list.Add(ParseValue(ref source, ref offset));
                        SkipWhitespace(ref source, ref offset);
                        if (source[offset] == ']') {
                            return list.ToArray();
                        } else {
                            offset++;
                        }
                    }
                    return list.ToArray();
                case '"':
                    offset++;
                    string result = ParseString(ref source, ref offset).Unescape();
                    while (offset < source.Length && source[offset] != ',' && source[offset] != ']' && source[offset] != '}') {
                        offset++;
                    }
                    return result;
                default:
                    string value = ParseUntil(ref source, ref offset, ',', ']', '}');
                    if (int.TryParse(value, out int intValue)) {
                        return intValue;
                    } else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue)) {
                        return doubleValue;
                    } else if (bool.TryParse(value, out bool boolValue)) {
                        return boolValue;
                    }
                    return value; // Unsupported types are handled as strings
            }
        }

        /// <summary>
        /// Write the value of an element to the JSON object under output.
        /// </summary>
        static void AppendValue(StringBuilder result, object value) {
            if (value is bool b) {
                result.Append(b.ToString().ToLowerInvariant());
            } else if (value is double d) {
                result.Append(d.ToString(CultureInfo.InvariantCulture));
            } else if (value is string str) {
                result.Append('"').Append(str.Escape()).Append('"');
            } else if (value is object[] array) {
                result.Append("[ ");
                for (int i = 0; i < array.Length; i++) {
                    if (i != 0) {
                        result.Append(", ");
                    }
                    AppendValue(result, array[i]);
                }
                result.Append(" ]");
            } else {
                result.Append(value);
            }
        }

        /// <summary>
        /// Jump to the next meaningful data point.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipWhitespace(ref string source, ref int offset) {
            while (offset < source.Length && char.IsWhiteSpace(source[offset])) {
                offset++;
            }
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Elements.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Add a new element to the current tree node.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string key, object value) => Add(key.Stores(value));

        /// <summary>
        /// Add a new element to the current tree node.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(KeyValuePair<string, object> element) => elements.Add(element);

        /// <inheritdoc/>
        public override string ToString() {
            StringBuilder result = new StringBuilder("{ ");
            for (int i = 0, c = elements.Count; i < c; i++) {
                if (i != 0) {
                    result.Append(", ");
                }
                result.Append('"').Append(elements[i].Key).Append("\": ");
                AppendValue(result, elements[i].Value);
                
            }
            return result.Append(" }").ToString();
        }
    }
}