using System;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// A command with a single integer parameter.
    /// </summary>
    abstract class IntegerCommand : Command {
        /// <summary>
        /// Number of parameters this command will use.
        /// </summary>
        public override int Parameters => 1;

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="value">The value supplied</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public abstract void Execute(int value, MainWindow app);

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="args">List of all calling arguments for the software</param>
        /// <param name="offset">The index of the first argument that is a parameter of this command</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public override void Execute(string[] args, int offset, MainWindow app) {
            if (int.TryParse(args[offset], out int value)) {
                Execute(value, app);
                return;
            }

            Console.Error.WriteLine($"Invalid parameter for {Name}, {args[offset]} is not an integer.");
            app.IsEnabled = false;
        }
    }
}