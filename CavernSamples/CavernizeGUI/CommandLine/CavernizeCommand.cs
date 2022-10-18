using System;

using CavernizeGUI.Resources;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Turns height generation from regular content on or off, up to 7.1.
    /// </summary>
    class CavernizeCommand : BooleanCommand {
        /// <summary>
        /// Full name of the command, including a preceding character like '-' if exists.
        /// </summary>
        public override string Name => "-upconvert";

        /// <summary>
        /// Shorthand for <see cref="Name"/>.
        /// </summary>
        public override string Alias => "-u";

        /// <summary>
        /// Description of the command that is displayed in the command list (help).
        /// </summary>
        public override string Help => "Turns height generation from regular content up to 7.1 on or off.";

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="value">The value supplied</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public override void Execute(bool value, MainWindow app) {
            if (app.Rendering) {
                Console.Error.WriteLine(string.Format(inProgress, "upconversion"));
                app.IsEnabled = false;
                return;
            }

            UpmixingSettings.Default.Cavernize = value;
        }
    }
}