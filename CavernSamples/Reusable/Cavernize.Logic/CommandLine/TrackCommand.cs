using Cavern.Format.Common;
using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Sets the active track for rendering.
/// </summary>
sealed class TrackCommand : IntegerCommand {
    /// <inheritdoc/>
    public override string Name => "-track";

    /// <inheritdoc/>
    public override string Alias => "-trk";

    /// <inheritdoc/>
    public override string Help => "Sets the active track for rendering by index.";

    /// <inheritdoc/>
    public override void Execute(int value, ICavernizeApp app) {
        IReadOnlyList<CavernizeTrack> audioTracks = app.LoadedFile.Tracks;
        if (value < 0 || value >= audioTracks.Count) {
            throw new CommandException($"Track index {value} is out of range (0-{audioTracks.Count - 1}).");
        }

        CavernizeTrack targetTrack = audioTracks[value];
        app.SelectedTrack = targetTrack;
        Console.WriteLine($"Active track set to: {targetTrack}");
    }
}
