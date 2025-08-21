using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine.BaseClasses;

/// <summary>
/// A command with a single integer parameter.
/// </summary>
public abstract class IntegerCommand : Command {
    /// <inheritdoc/>
    public sealed override int Parameters => 1;

    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="value">The value supplied</param>
    /// <param name="app">Reference to the application to perform setting changes and operations</param>
    public abstract void Execute(int value, ICavernizeApp app);

    /// <inheritdoc/>
    public sealed override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (int.TryParse(args[offset], out int value)) {
            Execute(value, app);
            return;
        }

        throw new CommandException($"Invalid parameter for {Name}, {args[offset]} is not an integer.");
    }
}
