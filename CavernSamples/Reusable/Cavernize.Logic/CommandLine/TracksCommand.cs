using Cavern.Format.Common;
using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// List each track contained in the loaded file.
/// </summary>
sealed class TracksCommand : Command {
    /// <inheritdoc/>
    public override string Name => "-tracks";

    /// <inheritdoc/>
    public override string Alias => "-trk";

    /// <inheritdoc/>
    public override int Parameters => 0;

    /// <inheritdoc/>
    public override string Help => "List each track contained in the loaded file.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        IReadOnlyList<CavernizeTrack> parsedTracks = app.LoadedFile.Tracks;
        CavernizeTrack first = parsedTracks.FirstOrDefault(x => x.Supported) ??
            throw new CommandException("At least one supported track is required to list tracks.");
        Track[] tracks = first.Track.Source.Tracks;

        for (int i = 0; i < tracks.Length; i++) {
            Track track = tracks[i];
            CavernizeTrack parsed = parsedTracks.FirstOrDefault(x => x.Track == track);
            if (parsed != null) {
                Console.WriteLine($"[{i}] {parsed}");
            } else {
                Console.WriteLine($"[{i}] {track.Format} (not decoded)");
            }
        }
        throw new CommandProcessingCanceledException();
    }
}
