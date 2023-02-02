namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Marks a command that won't show up under -help.
    /// </summary>
    abstract class HiddenCommand : Command {
        /// <summary>
        /// Hidden commands don't have shorthands.
        /// </summary>
        public override string Alias => string.Empty;
    }
}