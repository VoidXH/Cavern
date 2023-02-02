using System;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Lists all available commands.
    /// </summary>
    class HelpCommand : Command {
        /// <summary>
        /// Full name of the command, including a preceding character like '-' if exists.
        /// </summary>
        public override string Name => "-help";

        /// <summary>
        /// Shorthand for <see cref="Name"/>.
        /// </summary>
        public override string Alias => "-h";

        /// <summary>
        /// Number of parameters this command will use.
        /// </summary>
        public override int Parameters => 0;

        /// <summary>
        /// Description of the command that is displayed in the command list (help).
        /// </summary>
        public override string Help => "Lists all available commands.";

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="args">List of all calling arguments for the software</param>
        /// <param name="offset">The index of the first argument that is a parameter of this command</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public override void Execute(string[] args, int offset, MainWindow app) {
            Command[] pool = CommandPool;
            for (int i = 0; i < pool.Length; i++) {
                if (pool[i] is not HiddenCommand) {
                    Console.WriteLine($"{pool[i].Name} ({pool[i].Alias})\t: {pool[i].Help}");
                }
            }
            app.IsEnabled = false;
        }
    }
}