namespace EQAPOtoFIR {
    /// <summary>
    /// Time direction by the target filter.
    /// </summary>
    public enum ExportFormat {
        /// <summary>
        /// Forward time impulse export for convolution filters.
        /// </summary>
        Impulse,
        /// <summary>
        /// Inverse time impulse export for FIR filters.
        /// </summary>
        FIR
    }
}