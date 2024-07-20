using Cavern.Filters;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.Format.ConfigurationFile.Helpers {
    /// <summary>
    /// Placeholder where a <see cref="GraphicEQ"/> should be created.
    /// </summary>
    public sealed class LazyGraphicEQ : Filter, ILazyLoadableFilter {
        /// <summary>
        /// Desired frequency response change.
        /// </summary>
        readonly Equalizer equalizer;

        /// <summary>
        /// Sample rate at which this EQ is converted to a minimum-phase FIR filter.
        /// </summary>
        readonly int sampleRate;

        /// <summary>
        /// Placeholder where a <see cref="GraphicEQ"/> should be created.
        /// </summary>
        public LazyGraphicEQ(Equalizer equalizer, int sampleRate) {
            this.equalizer = equalizer;
            this.sampleRate = sampleRate;
        }

        /// <inheritdoc/>
        public override void Process(float[] samples) => throw new PlaceholderFilterException();

        /// <inheritdoc/>
        public Filter CreateFilter(FFTCachePool cachePool) {
            FFTCache cache = cachePool.Lease();
            Filter result = new GraphicEQ(equalizer, sampleRate, cache);
            cachePool.Return(cache);
            return result;
        }

        /// <inheritdoc/>
        public override object Clone() => new LazyGraphicEQ(equalizer, sampleRate);
    }
}