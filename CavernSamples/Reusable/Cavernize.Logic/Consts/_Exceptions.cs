namespace Cavernize.Logic; 

/// <summary>
/// Tells that an operation is already running.
/// </summary>
public class ConcurrencyException(string message) : Exception(message) {
}

/// <summary>
/// Tells that some applied settings are not compatible with each other.
/// </summary>
public class IncompatibleSettingsException(string message) : Exception(message) {
}

/// <summary>
/// Tells that a network operation has failed.
/// </summary>
public class NetworkException(string message) : Exception(message) {
}

/// <summary>
/// Tells that a track setup is wrong or unsupported.
/// </summary>
public class TrackException(string message) : Exception(message) {
}
