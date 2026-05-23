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
        IReadOnlyList<CavernizeTrack> parsedTracks = app.LoadedFile.Tracks;
        Track[] supportedTracks = parsedTracks.FirstOrDefault(x => x.Supported)?.Track.Source.Tracks ??
            throw new CommandException("At least one supported track is required to set a track.");

        if (value < 0 || value >= supportedTracks.Length) {
            throw new CommandException($"Track index {value} is out of range (0-{supportedTracks.Length - 1}).");
        }

        Track targetTrack = supportedTracks[value];
        CavernizeTrack selected = parsedTracks.FirstOrDefault(x => x.Track == targetTrack) ??
            throw new CommandException($"Track {value} is not supported and cannot be selected.");

        app.SelectedTrack = selected;
        Console.WriteLine($"Active track set to: {selected}");
    }
}
