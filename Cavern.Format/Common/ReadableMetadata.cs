using System.Collections.Generic;

namespace Cavern.Format.Common {
    /// <summary>
    /// A single field of a codec's metadata with its description.
    /// </summary>
    public readonly struct ReadableMetadataField {
        /// <summary>
        /// The name of the field by the codec specification.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// What the field actually contains in common terms.
        /// </summary>
        public readonly string description;

        /// <summary>
        /// Value of the field in the field's type.
        /// </summary>
        public readonly object value;

        /// <summary>
        /// A single field of a codec's metadata with its description.
        /// </summary>
        public ReadableMetadataField(string name, string description, object value) {
            this.name = name;
            this.description = description;
            this.value = value;
        }

        /// <summary>
        /// Displays the metadata field in human-readable format.
        /// </summary>
        public override string ToString() => $"{name} ({description}): {(value is IEnumerable<object> e ? string.Join(", ", e) : value)}";
    }

    /// <summary>
    /// Metadata of a codec in human-readable format, containing the names, descriptions, and values of fields in the codec header.
    /// </summary>
    public class ReadableMetadata {
        /// <summary>
        /// All the different headers used by the format, with their descriptive field infos.
        /// </summary>
        public (string name, IReadOnlyList<ReadableMetadataField> fields)[] Headers { get; }

        /// <summary>
        /// Box the human-readable <see cref="headers"/> for transfer.
        /// </summary>
        public ReadableMetadata((string name, IReadOnlyList<ReadableMetadataField> fields)[] headers) => Headers = headers;
    }
}