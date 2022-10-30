using System.Threading;

namespace Cavern.Filters {
    /// <summary>
    /// Extension functions for <see cref="Filter"/>s.
    /// </summary>
    public static class FilterExtensions {
        /// <summary>
        /// Process all channels of an interlaced <paramref name="target"/> stream with the <paramref name="filters"/> for each channel.
        /// </summary>
        public static void ProcessAllChannels(this Filter[] filters, float[] target) {
            int runs = filters.Length;
            using ManualResetEvent reset = new ManualResetEvent(false);
            for (int ch = 0; ch < filters.Length; ch++) {
                ThreadPool.QueueUserWorkItem(
                   new WaitCallback(channel => {
                       int ch = (int)channel;
                       filters[ch].Process(target, ch, filters.Length);
                       if (Interlocked.Decrement(ref runs) == 0) {
                           reset.Set();
                       }
                   }), ch);
            }
            reset.WaitOne();
        }
    }
}