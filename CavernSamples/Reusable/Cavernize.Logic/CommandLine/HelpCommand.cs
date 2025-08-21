using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Lists all available commands.
/// </summary>
sealed class HelpCommand : Command {
    /// <inheritdoc/>
    public override string Name => "-help";

    /// <inheritdoc/>
    public override string Alias => "-h";

    /// <inheritdoc/>
    public override int Parameters => 0;

    /// <inheritdoc/>
    public override string Help => "Lists all available commands.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        Command[] pool = CommandPool;
        for (int i = 0; i < pool.Length; i++) {
            if (pool[i] is not HiddenCommand) {
                Console.WriteLine($"{pool[i].Name} ({pool[i].Alias})\t: {pool[i].Help}");
            }
        }
        throw new CommandProcessingCanceledException();
    }
}
