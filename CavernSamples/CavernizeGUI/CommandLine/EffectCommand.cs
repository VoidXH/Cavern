using System;

using CavernizeGUI.Resources;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Sets height generation effect strength for the upconverter.
    /// </summary>
    class EffectCommand : IntegerCommand {
        /// <summary>
        /// Full name of the command, including a preceding character like '-' if exists.
        /// </summary>
        public override string Name => "-effect";

        /// <summary>
        /// Shorthand for <see cref="Name"/>.
        /// </summary>
        public override string Alias => "-e";

        /// <summary>
        /// Description of the command that is displayed in the command list (help).
        /// </summary>
        public override string Help => "Sets height generation effect strength for the upconverter in percent.";

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="value">The value supplied</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public override void Execute(int value, MainWindow app) {
            if (app.Rendering) {
                Console.Error.WriteLine(string.Format(inProgress, "effect"));
                app.IsEnabled = false;
                return;
            }

            UpmixingSettings.Default.Effect = value * .01f;
        }
    }
}