namespace Cavern.Format.Common {
    /// <summary>
    /// A codec that can supply its metadata in human-readable format.
    /// </summary>
    public interface IMetadataSupplier {
        /// <summary>
        /// Gets the metadata for this codec in a human-readable format.
        /// </summary>
        public ReadableMetadata GetMetadata();
    }
}