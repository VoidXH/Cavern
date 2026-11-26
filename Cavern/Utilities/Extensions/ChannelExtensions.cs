namespace Cavern.Utilities {
    /// <summary>
    /// Extension functions for <see cref="Channel"/>s.
    /// </summary>
    public static class ChannelExtensions {
        /// <summary>
        /// Get the number of channels above the horizon for a given channel layout.
        /// </summary>
        public static int GetOverheadChannelCount(this Channel[] channels) {
            int count = 0;
            for (int i = 0; i < channels.Length; i++) {
                if (channels[i].X < 0) {
                    count++;
                }
            }
            return count;
        }
    }
}