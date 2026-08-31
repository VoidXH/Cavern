namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Extension functions for <see cref="FIRChannelData"/>.
    /// </summary>
    public static class FIRChannelDataExtensions {
        /// <summary>
        /// Set up <see cref="FIRChannelData.OverrideIRCutoff"/> for all channels automatically detecting their type from system settings.
        /// </summary>
        public static void SetCutoffs(this FIRChannelData[] channels, int mainCutoff, int lfeCutoff) {
            for (int i = 0; i < channels.Length; i++) {
                channels[i].OverrideIRCutoff = Channel.IsLFE(i, channels.Length) ? lfeCutoff : mainCutoff;
            }
        }
    }
}
