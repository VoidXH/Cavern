namespace Cavernize.Logic.CommandLine;

/// <summary>
/// A command's execution has failed.
/// </summary>
public sealed class CommandException(string message) : Exception(message) { }

/// <summary>
/// Stop further command processing with no message.
/// </summary>
public sealed class CommandProcessingCanceledException : Exception { }
