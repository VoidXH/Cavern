using System.IO;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Opens a content file.
    /// </summary>
    class InputCommand : Command {
        /// <summary>
        /// Full name of the command, including a preceding character like '-' if exists.
        /// </summary>
        public override string Name => "-input";

        /// <summary>
        /// Shorthand for <see cref="Name"/>.
        /// </summary>
        public override string Alias => "-i";

        /// <summary>
        /// Number of parameters this command will use.
        /// </summary>
        public override int Parameters => 1;

        /// <summary>
        /// Description of the command that is displayed in the command list (help).
        /// </summary>
        public override string Help => "Opens a content file.";

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="args">List of all calling arguments for the software</param>
        /// <param name="offset">The index of the first argument that is a parameter of this command</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public override void Execute(string[] args, int offset, MainWindow app) {
            if (!File.Exists(args[offset])) {
                throw new CommandException($"The file \"{args[offset]}\" does not exist.");
            }
            app.OpenContent(args[offset]);
        }
    }
}