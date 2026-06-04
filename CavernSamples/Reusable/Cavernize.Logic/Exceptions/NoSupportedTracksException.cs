namespace Cavernize.Logic.Exceptions;

/// <summary>
/// Tells that at least one supported track is required to perform the operation.
/// </summary>
public class NoSupportedTracksException : Exception {
    const string message = "At least one supported track is required to perform this operation.";

    /// <summary>
    /// Tells that at least one supported track is required to perform the operation.
    /// </summary>
    public NoSupportedTracksException() : base(message) { }
}
