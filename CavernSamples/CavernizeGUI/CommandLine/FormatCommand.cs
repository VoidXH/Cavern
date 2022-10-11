using System;
using System.Linq;

using CavernizeGUI.Elements;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Selects the output audio codec.
    /// </summary>
    class FormatCommand : Command {
        /// <summary>
        /// Full name of the command, including a preceding character like '-' if exists.
        /// </summary>
        public override string Name => "-format";

        /// <summary>
        /// Shorthand for <see cref="Name"/>.
        /// </summary>
        public override string Alias => "-f";

        /// <summary>
        /// Number of parameters this command will use.
        /// </summary>
        public override int Parameters => 1;

        /// <summary>
        /// Description of the command that is displayed in the command list (help).
        /// </summary>
        public override string Help => "Selects the output audio codec.";

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="args">List of all calling arguments for the software</param>
        /// <param name="offset">The index of the first argument that is a parameter of this command</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public override void Execute(string[] args, int offset, MainWindow app) {
            if (app.Rendering) {
                Console.Error.WriteLine(string.Format(inProgress, "format"));
                app.IsEnabled = false;
                return;
            }

            ExportFormat[] formats = ExportFormat.Formats;
            for (int i = 0; i < formats.Length; i++) {
                if (args[offset].Equals(formats[i].Codec.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    args[offset].Equals(formats[i].FFName, StringComparison.OrdinalIgnoreCase)) {
                    app.audio.SelectedItem = formats[i];
                    return;
                }
            }

            string valids = string.Join(", ", formats.Select(x => x.Codec.ToString()));
            throw new CommandException($"Invalid output format ({args[offset]}). Valid options are: {valids}.");
        }
    }
}