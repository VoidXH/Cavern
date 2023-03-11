using System.Collections.Generic;

namespace Cavern.Format.Common {
    /// <summary>
    /// A single field of a codec's metadata with its description.
    /// </summary>
    public class ReadableMetadataField {
        /// <summary>
        /// The name of the field by the codec specification.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// What the field actually contains in common terms.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Value of the field in the field's type.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// A single field of a codec's metadata with its description.
        /// </summary>
        public ReadableMetadataField(string name, string description, object value) {
            Name = name;
            Description = description;
            Value = value;
        }

        /// <summary>
        /// Displays the metadata field in human-readable format.
        /// </summary>
        public override string ToString() => $"{Name} ({Description}): {(Value is IEnumerable<object> e ? string.Join(", ", e) : Value)}";
    }

    /// <summary>
    /// The intermediate hierarchy element of <see cref="ReadableMetadata"/>. Metadata is stored in headers, and headers contain fields.
    /// </summary>
    public class ReadableMetadataHeader {
        /// <summary>
        /// Name of the header.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The data values and their descriptions contained in this header.
        /// </summary>
        public IReadOnlyList<ReadableMetadataField> Fields { get; }

        /// <summary>
        /// Construct a holder of <paramref name="fields"/>.
        /// </summary>
        public ReadableMetadataHeader(string name, IReadOnlyList<ReadableMetadataField> fields) {
            Name = name;
            Fields = fields;
        }
    }

    /// <summary>
    /// Metadata of a codec in human-readable format, containing the names, descriptions, and values of fields in the codec header.
    /// </summary>
    public class ReadableMetadata {
        /// <summary>
        /// All the different headers used by the format, with their descriptive field infos.
        /// </summary>
        public IReadOnlyList<ReadableMetadataHeader> Headers { get; }

        /// <summary>
        /// Box the human-readable <see cref="Headers"/> for transfer.
        /// </summary>
        public ReadableMetadata(IReadOnlyList<ReadableMetadataHeader> headers) => Headers = headers;
    }
}