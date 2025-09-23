using Cavern.Format;

using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Language;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine.HiddenCommands;

/// <summary>
/// Overrides the PCM source of JOC encoded objects in E-AC-3 tracks.
/// </summary>
sealed class OverrideBedCommand : HiddenCommand {
    /// <inheritdoc/>
    public override string Name => "--override-bed";

    /// <inheritdoc/>
    public override int Parameters => 1;

    /// <inheritdoc/>
    public override string Help => "[Enhanced AC-3] Overrides the PCM source of JOC encoded objects.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) {
        if (!File.Exists(args[offset])) {
            throw new FileNotFoundException(args[offset]);
        }
        if (app.LoadedFile == null) {
            throw new CommandException("Set the input before setting the bed override.");
        }

        AudioReader overrider = AudioReader.Open(args[offset]);
        overrider.ReadHeader();

        OverrideBedFile file = new OverrideBedFile(app.LoadedFile.Path, overrider, new TrackStrings());
        if (file.Tracks[0].Renderer.Channels != overrider.ChannelCount) {
            throw new CommandException("Channel count of the overriding stream don't match the source stream.");
        }
        if (file.Tracks[0].SampleRate != overrider.SampleRate) {
            throw new CommandException("Sample rate of the overriding stream don't match the source stream.");
        }
        if (file.Tracks[0].Length != overrider.Length) {
            throw new CommandException("Length of the overriding stream don't match the source stream.");
        }
        file.Reset();
        app.OpenContent(file);
    }
}
