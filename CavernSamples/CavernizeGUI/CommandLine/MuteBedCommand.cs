using System;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Mutes the fixed bed channels.
    /// </summary>
    class MuteBedCommand : Command {
        /// <summary>
        /// Full name of the command, including a preceding character like '-' if exists.
        /// </summary>
        public override string Name => "-mute_bed";

        /// <summary>
        /// Shorthand for <see cref="Name"/>.
        /// </summary>
        public override string Alias => "-mb";

        /// <summary>
        /// Number of parameters this command will use.
        /// </summary>
        public override int Parameters => 0;

        /// <summary>
        /// Description of the command that is displayed in the command list (help).
        /// </summary>
        public override string Help => "Mutes the fixed bed channels.";

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="args">List of all calling arguments for the software</param>
        /// <param name="offset">The index of the first argument that is a parameter of this command</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public override void Execute(string[] args, int offset, MainWindow app) {
            if (app.Rendering) {
                Console.Error.WriteLine(string.Format(inProgress, "muting"));
                app.IsEnabled = false;
                return;
            }

            app.dynamicOnly.IsChecked = true;
        }
    }
}