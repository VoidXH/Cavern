using System.Collections.Generic;

namespace Cavern.Utilities {
    /// <summary>
    /// Extension methods for <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    public static class DictionaryExtensions {
        /// <summary>
        /// Get the key for a value that's found in the source <paramref name="dictionary"/>.
        /// </summary>
        public static TKey GetKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value) {
            foreach (KeyValuePair<TKey, TValue> pair in dictionary) {
                if (pair.Value.Equals(value)) {
                    return pair.Key;
                }
            }
            return default;
        }
    }
}