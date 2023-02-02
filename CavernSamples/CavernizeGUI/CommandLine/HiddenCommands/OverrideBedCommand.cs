using Cavern.Format;
using System.Diagnostics;
using System.IO;

namespace CavernizeGUI.CommandLine.HiddenCommands {
    internal class OverrideBedCommand : HiddenCommand {
        /// <summary>
        /// Full name of the command, including a preceding character like '-' if exists.
        /// </summary>
        public override string Name => "--override-bed";

        /// <summary>
        /// Number of parameters this command will use.
        /// </summary>
        public override int Parameters => 1;

        /// <summary>
        /// Description of the command that is displayed in the command list (help).
        /// </summary>
        public override string Help => "[Enhanced AC-3] Overrides the PCM source of JOC encoded objects.";

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="args">List of all calling arguments for the software</param>
        /// <param name="offset">The index of the first argument that is a parameter of this command</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
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