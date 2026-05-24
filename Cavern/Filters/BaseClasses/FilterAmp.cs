using System;
using System.Runtime.InteropServices;

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
        public IntPtr Handle { get; protected set; }

        /// <summary>
        /// When a <see cref="CavernAmp"/> filter is wrapped into a class that disposes it, don't allow further disposals.
        /// </summary>
        bool wrapped;

        /// <summary>
        /// A version of a <see cref="Filter"/> running in <see cref="CavernAmp"/>.
        /// </summary>
        public FilterAmp(IntPtr handle) => Handle = handle;

        /// <summary>
        /// Tell this wrapper that the filter was wrapped inside a class in <see cref="CavernAmp"/> that will dispose it in its destructor, so don't double free it.
        /// </summary>
        public void SetWrapped() => wrapped = true;

        /// <inheritdoc/>
        public override unsafe void Process(float[] samples) {
            fixed (float* pSamples = samples) {
                Filter_Process(Handle, pSamples, samples.Length);
            }
        }

        /// <inheritdoc/>
        public override unsafe void Process(float[] samples, int channel, int channels) {
            fixed (float* pSamples = samples) {
                Filter_ProcessChannel(Handle, pSamples, samples.Length, channel, channels);
            }
        }

        /// <inheritdoc/>
        public override object Clone() => new FilterAmp(Filter_Clone(Handle));

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is FilterAmp other && Handle == other.Handle;

        /// <inheritdoc/>
        public bool Equals(FilterAmp other) => Handle == other.Handle;

        /// <inheritdoc/>
        public override int GetHashCode() => Handle.GetHashCode();

        /// <inheritdoc/>
        public void Dispose() {
            if (Handle != IntPtr.Zero) {
                if (!wrapped) {
                    Filter_Dispose(Handle);
                }
                Handle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Free up native resources when the object wasn't disposed.
        /// </summary>
        ~FilterAmp() => Dispose();

        /// <summary>
        /// Create a copy of the filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        protected static extern IntPtr Filter_Clone(IntPtr instance);

        /// <summary>
        /// pply a filter to an array of samples (single channel).
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern unsafe void Filter_Process(IntPtr instance, float* samples, int len);

        /// <summary>
        /// Apply a filter to an array of samples (interleaved channels).
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern unsafe void Filter_ProcessChannel(IntPtr instance, float* samples, int len, int channel, int channels);

        /// <summary>
        /// Dispose the filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void Filter_Dispose(IntPtr instance);
    }
}
