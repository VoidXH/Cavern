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
    public override string Alias => "-trks";

    /// <inheritdoc/>
    public override int Parameters => 0;

    /// <inheritdoc/>
    public override string Help => "List each track contained in the loaded file.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        IReadOnlyList<CavernizeTrack> audioTracks = app.LoadedFile.Tracks;
        if (audioTracks.All(track => track.Track == null)) {
            for (int i = 0; i < audioTracks.Count; i++) {
                Console.WriteLine($"[{i}] {audioTracks[i]}");
            }
            throw new CommandProcessingCanceledException();
        }

        IReadOnlyList<Track> allTracks = app.LoadedFile.AllTracks;
        for (int i = 0, audioIndex = 0, c = allTracks.Count; i < c; i++) {
            CavernizeTrack parsed = audioTracks.FirstOrDefault(x => x.Track == allTracks[i]);
            if (parsed != null) {
                Console.WriteLine($"[{audioIndex++}] {parsed}");
            } else {
                Console.WriteLine($"[X] {allTracks[i].Format} (not audio)");
            }
        }
        throw new CommandProcessingCanceledException();
    }
}
