using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine.BaseClasses;

/// <summary>
/// A command that can be either on/yes/true or off/no/false.
/// </summary>
public abstract class BooleanCommand : Command {
    /// <inheritdoc/>
    public sealed override int Parameters => 1;

    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="value">The value supplied</param>
    /// <param name="app">Reference to the application to perform setting changes and operations</param>
    public abstract void Execute(bool value, ICavernizeApp app);

    /// <inheritdoc/>
    public sealed override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (bool.TryParse(args[offset], out bool value)) {
            Execute(value, app);
            return;
        }
        if (args[offset].ToLowerInvariant().Equals(true1) ||
            args[offset].ToLowerInvariant().Equals(true2)) {
            Execute(true, app);
            return;
        }
        if (args[offset].ToLowerInvariant().Equals(false1) ||
            args[offset].ToLowerInvariant().Equals(false2)) {
            Execute(false, app);
            return;
        }

        throw new CommandException($"Invalid parameter for {Name} ({args[offset]}). Use either \"on\" or \"off\".");
    }

    /// <summary>
    /// A possible value for a true parameter.
    /// </summary>
    const string true1 = "on", true2 = "yes";

    /// <summary>
    /// A possible value for a false parameter.
    /// </summary>
    const string false1 = "off", false2 = "no";
}
