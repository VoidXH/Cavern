using Cavern.Utilities;

namespace CavernizeGUI.CommandLine.HiddenCommands {
    /// <summary>
    /// Adds additional gain to renders.
    /// </summary>
    class RenderGain : UnsafeCommand {
        /// <inheritdoc/>
        public override string Name => "--render-gain";

        /// <inheritdoc/>
        public override int Parameters => 1;

        /// <inheritdoc/>
        public override string Help => "Applies additional gain to the content volume in decibels.";

        /// <inheritdoc/>
        public override void Execute(string[] args, int offset, MainWindow app) {
            if (!QMath.TryParseFloat(args[offset], out float attenuation)) {
                throw new CommandException($"The provided render gain ({args[offset]}) is not a number.");
            }
            app.renderGain = QMath.DbToGain(attenuation);
        }
    }
}