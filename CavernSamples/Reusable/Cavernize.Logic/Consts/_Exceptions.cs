namespace Cavernize.Logic; 

/// <summary>
/// Tells that a network operation has failed.
/// </summary>
public class NetworkException(string message) : Exception(message) {
}
