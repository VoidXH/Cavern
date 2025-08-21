using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Selects the output audio codec.
/// </summary>
public sealed class FormatCommand : Command {
    /// <inheritdoc/>
    public override string Name => "-format";

    /// <inheritdoc/>
    public override string Alias => "-f";

    /// <inheritdoc/>
    public override int Parameters => 1;

    /// <inheritdoc/>
    public override string Help => "Selects the output audio codec.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (app.Rendering) {
            InProgressError(app, "format");
        }

        ExportFormat[] formats = ExportFormat.GetFormats(new Language.TrackStrings());
        for (int i = 0; i < formats.Length; i++) {
            if (args[offset].Equals(formats[i].Codec.ToString(), StringComparison.OrdinalIgnoreCase) ||
                args[offset].Equals(formats[i].FFName, StringComparison.OrdinalIgnoreCase)) {
                app.ExportFormat = formats[i];
                return;
            }
        }

        string valids = string.Join(", ", formats.Select(x => x.Codec.ToString()));
        throw new CommandException($"Invalid output format ({args[offset]}). Valid options are: {valids}.");
    }
}
