using System;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// A command that can be either on/yes/true or off/no/false.
    /// </summary>
    abstract class BooleanCommand : Command {
        /// <summary>
        /// Number of parameters this command will use.
        /// </summary>
        public override int Parameters => 1;

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="value">The value supplied</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public abstract void Execute(bool value, MainWindow app);

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="args">List of all calling arguments for the software</param>
        /// <param name="offset">The index of the first argument that is a parameter of this command</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public override void Execute(string[] args, int offset, MainWindow app) {
            if (bool.TryParse(args[offset], out bool value)) {
                Execute(value, app);
                return;
            }
            if (args[offset].ToLower() == "on" || args[offset].ToLower() == "yes") {
                Execute(true, app);
                return;
            }
            if (args[offset].ToLower() == "off" || args[offset].ToLower() == "no") {
                Execute(false, app);
                return;
            }

            Console.Error.WriteLine($"Invalid parameter for {Name} ({args[offset]}). Use either \"on\" or \"off\".");
            app.IsEnabled = false;
        }
    }
}