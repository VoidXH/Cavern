namespace CavernPipeServer.Logic;

/// <summary>
/// Tells if CavernPipe is already running.
/// </summary>
public sealed class CavernPipeAlreadyRunningException(bool restarting) : Exception(restarting ? restartMessage : runningMessage) {
    /// <summary>
    /// The server is currently restarting, this is why it's not available.
    /// </summary>
    public bool Restarting { get; } = restarting;

    /// <summary>
    /// This error message is thrown when CavernPipe is already initializing by a different operation/host.
    /// </summary>
    const string restartMessage = "CavernPipe's restart is already in progress.";

    /// <summary>
    /// This error message is thrown when the pipe can't be created.
    /// </summary>
    const string runningMessage = "CavernPipe is already running, or a pipe called CavernPipe was created by a different application.";
}

/// <summary>
/// Tells if CavernPipe failed to launch in the hardcoded timeout.
/// </summary>
public sealed class CavernPipeLaunchTimeoutException() : Exception($"CavernPipe failed to start in {PipeHandler.timeout} seconds.");
