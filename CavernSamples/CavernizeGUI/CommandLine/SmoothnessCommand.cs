using System;

using CavernizeGUI.Resources;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Sets generated object movement smoothness for the upconverter.
    /// </summary>
    class SmoothnessCommand : IntegerCommand {
        /// <summary>
        /// Full name of the command, including a preceding character like '-' if exists.
        /// </summary>
        public override string Name => "-smooth";

        /// <summary>
        /// Shorthand for <see cref="Name"/>.
        /// </summary>
        public override string Alias => "-s";

        /// <summary>
        /// Description of the command that is displayed in the command list (help).
        /// </summary>
        public override string Help => "Sets generated object movement smoothness for the upconverter in percent.";

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="value">The value supplied</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public override void Execute(int value, MainWindow app) {
            if (app.Rendering) {
                Console.Error.WriteLine(string.Format(inProgress, "smoothness"));
                app.IsEnabled = false;
                return;
            }

            UpmixingSettings.Default.Smoothness = value * .01f;
        }
    }
}