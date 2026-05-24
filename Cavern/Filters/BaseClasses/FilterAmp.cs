using System;

using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// A version of a <see cref="Filter"/> running in <see cref="CavernAmp"/>.
    /// </summary>
    public class FilterAmp : Filter, IDisposable, IEquatable<FilterAmp> {
        /// <summary>
        /// Reference to the CavernAmp filter instance.
        /// </summary>
        /// <remarks>This should be disposed of when the filter is no longer needed, otherwise memory leaks may occur.</remarks>
        public IntPtr Handle { get; }

        /// <summary>
        /// When a <see cref="CavernAmp"/> filter is wrapped into a class that disposes it, don't allow further disposals.
        /// </summary>
        bool wrapped;

        /// <summary>
        /// The <see cref="Handle"/> was freed.
        /// </summary>
        bool disposed;

        /// <summary>
        /// A version of a <see cref="Filter"/> running in <see cref="CavernAmp"/>.
        /// </summary>
        public FilterAmp(IntPtr handle) => Handle = handle;

        /// <summary>
        /// Tell this wrapper that the filter was wrapped inside a class in <see cref="CavernAmp"/> that will dispose it in its destructor, so don't double free it.
        /// </summary>
        public void SetWrapped() => wrapped = true;

        /// <inheritdoc/>
        public override void Process(float[] samples) => CavernAmp.Filter_Process(Handle, samples);

        /// <inheritdoc/>
        public override void Process(float[] samples, int channel, int channels) => CavernAmp.Filter_Process(Handle, samples, channel, channels);

        /// <inheritdoc/>
        public override object Clone() => new FilterAmp(CavernAmp.Filter_Clone(Handle));

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is FilterAmp other && Handle == other.Handle;

        /// <inheritdoc/>
        public bool Equals(FilterAmp other) => Handle == other.Handle;

        /// <inheritdoc/>
        public override int GetHashCode() => Handle.GetHashCode();

        /// <inheritdoc/>
        public void Dispose() {
            if (!disposed) {
                if (!wrapped) {
                    CavernAmp.Filter_Dispose(Handle);
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Free up native resources when the object wasn't disposed.
        /// </summary>
        ~FilterAmp() => Dispose();
    }
}
