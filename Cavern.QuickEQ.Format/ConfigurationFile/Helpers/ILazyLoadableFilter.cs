using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Format.ConfigurationFile.Helpers {
    /// <summary>
    /// The <see cref="Filter"/> would be mildly computationally heavy to construct, increasing loading times. Hold on to the raw data,
    /// and recreate the filter in parallel when a configuration file was completely parsed.
    /// </summary>
    public interface ILazyLoadableFilter {
        /// <summary>
        /// Create the actual filter that should replace this placeholder.
        /// </summary>
        /// <param name="cachePool">If the filter performs FFT, parallel creation can use <see cref="FFTCachePool"/>s</param>
        Filter CreateFilter(FFTCachePool cachePool);
    }
}