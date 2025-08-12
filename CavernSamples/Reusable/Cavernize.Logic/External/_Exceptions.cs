namespace Cavernize.Logic.External;

/// <summary>
/// Tells if no valid release was found while searching.
/// </summary>
public class NoValidReleaseException : Exception {
    const string message = "No valid release was found while searching.";

    /// <summary>
    /// Tells if no valid release was found while searching.
    /// </summary>
    public NoValidReleaseException() : base(message) { }
}
