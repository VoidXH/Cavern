namespace Cavern.Format.Common {
    /// <summary>
    /// Extension methods for <see cref="Track"/>s.
    /// </summary>
    public static class TrackExtensions {
        /// <summary>
        /// Find the first track with the given ID in the tracklist. If found, the index is returned, -1 otherwise.
        /// </summary>
        public static int GetIndexByID(this Track[] tracks, long id) {
            for (int i = 0; i < tracks.Length; i++) {
                if (tracks[i].ID == id) {
                    return i;
                }
            }
            return -1;
        }
    }
}