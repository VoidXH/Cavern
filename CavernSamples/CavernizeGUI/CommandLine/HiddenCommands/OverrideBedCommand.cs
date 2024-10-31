using System.IO;

using Cavern.Format;

namespace CavernizeGUI.CommandLine.HiddenCommands {
    /// <summary>
    /// Overrides the PCM source of JOC encoded objects in E-AC-3 tracks.
    /// </summary>
    internal class OverrideBedCommand : HiddenCommand {
        /// <inheritdoc/>
        public override string Name => "--override-bed";

        /// <inheritdoc/>
        public override int Parameters => 1;

        /// <inheritdoc/>
        public override string Help => "[Enhanced AC-3] Overrides the PCM source of JOC encoded objects.";

        /// <inheritdoc/>
        public override void Execute(string[] args, int offset, MainWindow app) {
            if (!File.Exists(args[offset])) {
                throw new FileNotFoundException(args[offset]);
            }
            if (app.FilePath == null) {
                throw new CommandException("Set the input before setting the bed override.");
            }

            AudioReader overrider = AudioReader.Open(args[offset]);
            overrider.ReadHeader();

            OverrideBedFile file = new OverrideBedFile(app.FilePath, overrider);
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
            app.SetFile(file);
        }
    }
}