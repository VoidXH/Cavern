namespace Cavern.Channels {
    /// <summary>
    /// Extension methods for <see cref="Channel"/> arrays.
    /// </summary>
    public static class ChannelArrayExtensions {
        /// <summary>
        /// Gets the count of channels on the left and right sides.
        /// </summary>
        /// <param name="channels">Array of channels to analyze.</param>
        /// <returns>A tuple containing the number of channels to the left and to the right. LFE and centerline channels are excluded.</returns>
        public static (int left, int right) GetSideChannels(Channel[] channels) {
            if (channels == null) {
                return (1, 1);
            }

            int leftChannels = 0;
            int rightChannels = 0;
            for (int i = 0; i < channels.Length; i++) {
                Channel current = channels[i];
                if (current == null || current.LFE) {
                    continue;
                }

                if (current.Y < 0) {
                    leftChannels++;
                } else if (current.Y > 0) {
                    rightChannels++;
                }
            }

            if (leftChannels == 0) {
                leftChannels = 1;
            }
            if (rightChannels == 0) {
                rightChannels = 1;
            }

            return (leftChannels, rightChannels);
        }
    }
}
