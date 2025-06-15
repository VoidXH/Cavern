using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cavern.Format.JSON {
    /// <summary>
    /// Extensions making it easy to work with and generate JSON data.
    /// </summary>
    public static class JsonFileExtensions {
        /// <summary>
        /// A shorthand as <see cref="string"/>.Stores(<see cref="object"/>) to create a <see cref="KeyValuePair"/> used in Cavern's JSON parser.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyValuePair<string, object> Stores(this string key, object value) => new KeyValuePair<string, object>(key, value);
    }
}
