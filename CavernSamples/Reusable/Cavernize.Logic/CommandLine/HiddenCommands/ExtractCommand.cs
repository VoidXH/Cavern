using Cavern.Format.Common;

using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine.HiddenCommands;

/// <summary>
/// Extracts the raw bytes of the track with the given index, regardless if it's supported.
/// </summary>
sealed class ExtractCommand : UnsafeCommand {
    /// <inheritdoc/>
    public override string Name => "--extract";

    /// <inheritdoc/>
    public override int Parameters => 2;

    /// <inheritdoc/>
    public override string Help => "Extracts the raw bytes of the track with the given index (starting from 0), regardless if it's supported.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        CavernizeTrack first = app.LoadedFile.Tracks.FirstOrDefault(x => x.Supported) ??
            throw new CommandException("At least one supported track is required to list tracks.");
        Track[] tracks = first.Track.Source.Tracks;

        if (!int.TryParse(args[offset], out int index) || index < 0 || index >= tracks.Length) {
            throw new CommandException($"The provided index ({args[offset]}) is invalid.");
        }

        Track track = tracks[index];
        using FileStream output = File.Create(args[offset + 1]);
        string fileName = Path.GetFileName(args[offset + 1]);
        Console.WriteLine($"Extracting track {index} to \"{fileName}\"...");
        while (track.IsNextBlockAvailable()) {
            byte[] block = track.ReadNextBlock();
            output.Write(block, 0, block.Length);
        }
        throw new CommandProcessingCanceledException();
    }
}
