namespace Cavern.Utilities {
    /// <summary>
    /// An operation that takes a long time and should run in the background. Its interface provides progress polling.
    /// </summary>
    public interface ILongProcess {
        /// <summary>
        /// The ratio of doneness [0;1].
        /// </summary>
        float Progress { get; }
    }
}