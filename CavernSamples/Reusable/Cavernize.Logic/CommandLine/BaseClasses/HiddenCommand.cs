namespace Cavernize.Logic.CommandLine.BaseClasses;

/// <summary>
/// Marks a command that won't show up under -help.
/// </summary>
public abstract class HiddenCommand : Command {
    /// <summary>
    /// Hidden commands don't have shorthands.
    /// </summary>
    public sealed override string Alias => string.Empty;
}
