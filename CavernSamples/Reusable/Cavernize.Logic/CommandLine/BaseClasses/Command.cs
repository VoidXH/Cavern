using Cavernize.Logic.CommandLine.HiddenCommands;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine.BaseClasses;

/// <summary>
/// Abstract command line parameter.
/// </summary>
public abstract class Command {
    /// <summary>
    /// An instance of all usable commands.
    /// </summary>
    public static Command[] CommandPool {
        get {
            commandPool ??= [
                new HelpCommand(),
                new InputCommand(),
                new FormatCommand(),
                new TargetCommand(),
                new OutputCommand(),
                new MuteBedCommand(),
                new MuteGroundCommand(),
                new MatrixCommand(),
                new CavernizeCommand(),
                new EffectCommand(),
                new SmoothnessCommand(),
                new SpeakerVirtualizerCommand(),

                // Hidden commands
                new OverrideBedCommand(),
                new RenderGain(),
                new UnsafeCommand(),
            ];
            return commandPool;
        }
    }

    /// <summary>
    /// Created command instances if any command is called.
    /// </summary>
    static Command[] commandPool;

    /// <summary>
    /// Full name of the command, including a preceding character like '-' if exists.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Shorthand for <see cref="Name"/>.
    /// </summary>
    public abstract string Alias { get; }

    /// <summary>
    /// Number of parameters this command will use.
    /// </summary>
    public abstract int Parameters { get; }

    /// <summary>
    /// Description of the command that is displayed in the command list (help).
    /// </summary>
    public abstract string Help { get; }

    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="args">List of all calling arguments for the software</param>
    /// <param name="offset">The index of the first argument that is a parameter of this command</param>
    /// <param name="app">Reference to the application to perform setting changes and operations</param>
    public abstract void Execute(string[] args, int offset, ICavernizeApp app);

    /// <summary>
    /// Get the command an argument called.
    /// </summary>
    public static Command GetCommandByArgument(string argument) {
        for (int i = 0, c = CommandPool.Length; i < c; i++) {
            if (argument.Equals(commandPool[i].Name) || argument.Equals(commandPool[i].Alias)) {
                return commandPool[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Throw an exception that conversion is already in progress, and stop further processing of the command line arguments.
    /// </summary>
    protected static void InProgressError(ICavernizeApp app, string setting) =>
        throw new CommandException(string.Format(inProgress, setting));

    /// <summary>
    /// Error message when rendering is already in progress and the user is trying to change something.
    /// </summary>
    const string inProgress = "Rendering was already in progress, {0} can only be changed before the " +
        "-output (-o) argument. The export was cancelled and temporary files should be removed manually.";
}
