using System;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// A command's execution has failed.
    /// </summary>
    public class CommandException : Exception {
        /// <summary>
        /// A command's execution has failed.
        /// </summary>
        public CommandException(string message) : base(message) { }
    }
}