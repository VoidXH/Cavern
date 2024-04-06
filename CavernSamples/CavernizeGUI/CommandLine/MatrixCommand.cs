using System;

using CavernizeGUI.Resources;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Turns 7.1-creation from channel-based sources with less channels on or off.
    /// </summary>
    class MatrixCommand : BooleanCommand {
        /// <inheritdoc/>
        public override string Name => "-matrix";

        /// <inheritdoc/>
        public override string Alias => "-mx";

        /// <inheritdoc/>
        public override string Help => "Turns 7.1-creation from channel-based sources with less channels on or off.";

        /// <inheritdoc/>
        public override void Execute(bool value, MainWindow app) {
            if (app.Rendering) {
                Console.Error.WriteLine(string.Format(inProgress, "matrixing"));
                app.IsEnabled = false;
                return;
            }

            UpmixingSettings.Default.MatrixUpmix = value;
        }
    }
}