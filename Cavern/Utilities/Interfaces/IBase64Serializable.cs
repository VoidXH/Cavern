using System;

namespace Cavern.Utilities {
    /// <summary>
    /// An object that can be serialized to and deserialized from a base64 string.
    /// </summary>
    public interface IBase64Serializable {
        /// <summary>
        /// Serialize this object to a base64 string.
        /// </summary>
        string ToBase64();

        /// <summary>
        /// Deserialize this object from a base64 string.
        /// </summary>
        void FromBase64(string source);
    }
}
