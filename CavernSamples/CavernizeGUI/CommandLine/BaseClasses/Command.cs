using CavernizeGUI.CommandLine.HiddenCommands;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Abstract command line parameter.
    /// </summary>
    abstract class Command {
        /// <summary>
        /// An instance of all usable commands.
        /// </summary>
        public static Command[] CommandPool {
            get {
                commandPool ??= new Command[] {
                    new HelpCommand(),
                    new InputCommand(),
                    new FormatCommand(),
                    new TargetCommand(),
                    new OutputCommand(),
                    new MuteBedCommand(),
                    new MuteGroundCommand(),
                    new MatrixCommand(),
                    new CavernizeCommand(),
                    new EffectCommand(),
                    new SmoothnessCommand(),

                    // Hidden commands
                    new OverrideBedCommand(),
                };
                return commandPool;
            }
        }

        /// <summary>
        /// Created command instances if any command is called.
        /// </summary>
        static Command[] commandPool;

        /// <summary>
        /// Full name of the command, including a preceding character like '-' if exists.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Shorthand for <see cref="Name"/>.
        /// </summary>
        public abstract string Alias { get; }

        /// <summary>
        /// Number of parameters this command will use.
        /// </summary>
        public abstract int Parameters { get; }

        /// <summary>
        /// Description of the command that is displayed in the command list (help).
        /// </summary>
        public abstract string Help { get; }

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="args">List of all calling arguments for the software</param>
        /// <param name="offset">The index of the first argument that is a parameter of this command</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public abstract void Execute(string[] args, int offset, MainWindow app);

        /// <summary>
        /// Get the command an argument called.
        /// </summary>
        public static Command GetCommandByArgument(string argument) {
            for (int i = 0, c = CommandPool.Length; i < c; i++) {
                if (argument.Equals(commandPool[i].Name) || argument.Equals(commandPool[i].Alias)) {
                    return commandPool[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Error message when rendering is already in progress and the user is trying to change something.
        /// </summary>
        protected const string inProgress = "Rendering was already in progress, {0} can only be changed before the " +
            "-output (-o) argument. The export was cancelled and temporary files should be removed manually.";
    }
}