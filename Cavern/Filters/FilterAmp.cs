using System;

namespace Cavern.Filters {
    /// <summary>
    /// A version of a <see cref="Filter"/> running in <see cref="Cavern.Utilities.CavernAmp"/>.
    /// </summary>
    public abstract class FilterAmp : Filter, IDisposable {
        /// <summary>
        /// Reference to the CavernAmp filter instance.
        /// </summary>
        /// <remarks>This should be disposed of when the filter is no longer needed, otherwise memory leaks may occur.</remarks>
        public IntPtr Handle { get; }

        /// <summary>
        /// A version of a <see cref="Filter"/> running in <see cref="Cavern.Utilities.CavernAmp"/>.
        /// </summary>
        protected FilterAmp(IntPtr handle) => Handle = handle;

        /// <inheritdoc/>
        public override abstract object Clone();

        /// <inheritdoc/>
        public abstract void Dispose();
    }
}
