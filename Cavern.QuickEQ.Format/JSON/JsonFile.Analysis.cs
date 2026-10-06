using System.Collections.Generic;
using System.Linq;

namespace Cavern.Format.JSON {
    partial class JsonFile {
        /// <summary>
        /// Checks if every element of the <paramref name="baseFile"/> is found in this file.
        /// </summary>
        public bool IsSupersetOf(JsonFile baseFile) => IsSupersetOf(baseFile, false);

        /// <summary>
        /// Checks if every element of the <paramref name="baseFile"/> is found in this file. Optionally throws a <see cref="KeyNotFoundException"/> if an element is missing.
        /// </summary>
        public bool IsSupersetOf(JsonFile baseFile, bool throwOnMissing) => IsSupersetOf(baseFile, throwOnMissing, null);

        /// <summary>
        /// Checks if every element of the <paramref name="baseFile"/> is found in this file, allowing the keys in <paramref name="exceptions"/> to be missing at any level.
        /// Optionally throws a <see cref="KeyNotFoundException"/> if a non-exception element is missing.
        /// </summary>
        public bool IsSupersetOf(JsonFile baseFile, bool throwOnMissing, IReadOnlyCollection<string> exceptions) {
            IReadOnlyList<KeyValuePair<string, object>> baseElements = baseFile.Elements;
            for (int i = 0, baseCount = baseElements.Count; i < baseCount; i++) {
                KeyValuePair<string, object> baseElement = baseElements[i];
                int match = -1;
                for (int j = 0, count = elements.Count; j < count; j++) {
                    if (baseElement.Key == elements[j].Key) {
                        match = j;
                        break;
                    }
                }

                if (match < 0) {
                    if (exceptions != null && exceptions.Contains(baseElement.Key)) {
                        continue;
                    }

                    if (throwOnMissing) {
                        throw new KeyNotFoundException($"\"{baseElement.Key}\" was not found.");
                    }

                    return false;
                }

                if (elements[match].Value is JsonFile thisValue && baseElement.Value is JsonFile baseValue && !thisValue.IsSupersetOf(baseValue, throwOnMissing, exceptions)) {
                    if (throwOnMissing) {
                        throw new KeyNotFoundException($"\"{baseElement.Key}\" was not found.");
                    }

                    return false;
                }
            }
            return true;
        }
    }
}
