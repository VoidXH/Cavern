using System.Collections.Generic;

namespace Cavern.Format.JSON {
    partial class JsonFile {
        /// <summary>
        /// Checks if every element of the <paramref name="baseFile"/> is found in this file.
        /// </summary>
        public bool IsSupersetOf(JsonFile baseFile) => IsSupersetOf(baseFile, false);

        /// <summary>
        /// Checks if every element of the <paramref name="baseFile"/> is found in this file. Optionally throws a <see cref="KeyNotFoundException"/> if an element is missing.
        /// </summary>
        public bool IsSupersetOf(JsonFile baseFile, bool throwOnMissing) {
            IReadOnlyList<KeyValuePair<string, object>> baseElements = baseFile.Elements;
            for (int i = 0, baseCount = baseElements.Count; i < baseCount; i++) {
                bool contained = false;
                for (int j = 0, count = elements.Count; j < count; j++) {
                    if (baseElements[i].Key == elements[j].Key) {
                        contained = true;
                        break;
                    }
                }

                if (!contained ||
                    (elements[i].Value is JsonFile thisValue && baseElements[i].Value is JsonFile baseValue && !thisValue.IsSupersetOf(baseValue, throwOnMissing))) {
                    if (throwOnMissing) {
                        throw new KeyNotFoundException($"\"{baseElements[i].Key}\" was not found.");
                    }

                    return false;
                }
            }
            return true;
        }
    }
}
