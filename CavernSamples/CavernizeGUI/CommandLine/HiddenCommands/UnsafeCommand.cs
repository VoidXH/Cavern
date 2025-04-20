using Cavern.Format.Common;

namespace CavernizeGUI.CommandLine.HiddenCommands {
    /// <summary>
    /// Disables checks for conditions that don't inherently break operation, but are mandated by standards.
    /// </summary>
    class UnsafeCommand : HiddenCommand {
        /// <inheritdoc/>
        public override string Name => "--unsafe";

        /// <inheritdoc/>
        public override int Parameters => 0;

        /// <inheritdoc/>
        public override string Help => "Disables checks for conditions that are mandated by standards but might don't break decoding.";

        /// <inheritdoc/>
        public override void Execute(string[] args, int offset, MainWindow app) => CavernFormatGlobal.Unsafe = true;
    }
}