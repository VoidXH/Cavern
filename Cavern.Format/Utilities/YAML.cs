using System;
using System.Collections.Generic;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Represents an object of named key-value pairs where the value can be a string, another object, or an array of objects.
    /// </summary>
    internal class YAMLObject : Dictionary<string, object> { }

    /// <summary>
    /// YAML file representation, the entire file is in memory.
    /// </summary>
    internal class YAML {
        /// <summary>
        /// Root element of the YAML file.
        /// </summary>
        public YAMLObject Data { get; } = new YAMLObject();

        /// <summary>
        /// Parse a YAML file from a string.
        /// </summary>
        public YAML(string contents) {
            string[] lines = contents.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            // The holder is a YAMLObject when it's an object of key-value pair descriptors, or List<YAMLObject> when it's a list
            Stack<(int indent, object holder)> layers = new Stack<(int, object)>();
            layers.Push((0, Data));
            string lastKey = null;
            foreach (string line in lines) {
                int currentIndent = 0;
                while (currentIndent < line.Length && line[currentIndent] == ' ') {
                    currentIndent++;
                }
                if (currentIndent == line.Length) {
                    continue; // Skip empty lines
                }

                int split = line.IndexOf(':');
                if (split == -1) {
                    continue; // Empty arrays or invalid lines
                }
                string key = line[..split].Trim();
                string value = line[(split + 1)..].TrimStart();

                (int lastIndent, object holder) = layers.Peek();
                while (currentIndent < lastIndent) { // Going to outer layers
                    layers.Pop();
                    (lastIndent, holder) = layers.Peek();
                }

                if (key[0] == '-') { // Array item start
                    List<YAMLObject> list;
                    if (currentIndent != lastIndent) { // Array entry not yet made
                        list = new List<YAMLObject>();
                        ((YAMLObject)holder)[lastKey] = list;
                        layers.Push((currentIndent, list));
                    } else {
                        list = (List<YAMLObject>)holder;
                    }
                    holder = new YAMLObject();
                    list.Add((YAMLObject)holder);
                    currentIndent += CutArrayIndent(ref key);
                    layers.Push((currentIndent, holder));
                }

                if (value.Length != 0) {
                    ((YAMLObject)holder)[key] = value;
                }
                lastKey = key;
            }
        }

        /// <summary>
        /// Cuts the indentation of an array line, returning the number of cut characters (how much deeper is the actual indentation).
        /// </summary>
        int CutArrayIndent(ref string line) {
            int indent = 1;
            while (indent < line.Length && line[indent] == ' ') {
                indent++;
            }
            line = line[indent..];
            return indent;
        }
    }
}
