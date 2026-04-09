using Cavernize.Logic.CavernSettings;
using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine.HiddenCommands;

/// <summary>
/// Makes Cavernize not delete intermediate files when using external converters.
/// </summary>
class KeepTempFilesCommand : HiddenCommand {
    /// <inheritdoc/>
    public override string Name => "--keep-temp-files";

    /// <inheritdoc/>
    public override int Parameters => 0;

    /// <inheritdoc/>
    public override string Help => "Makes Cavernize not delete intermediate files when using external converters.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) => RenderingSettings.KeepTempFiles = true;
}
