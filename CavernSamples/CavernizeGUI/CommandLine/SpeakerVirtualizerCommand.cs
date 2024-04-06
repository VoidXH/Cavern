using System;

using CavernizeGUI.Resources;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Virtualizes and downmixes elevated objects to ground-only layouts.
    /// </summary>
    class SpeakerVirtualizerCommand : BooleanCommand {
        /// <inheritdoc/>
        public override string Name => "-speaker_virt";

        /// <inheritdoc/>
        public override string Alias => "-sv";

        /// <inheritdoc/>
        public override string Help => "Virtualizes and downmixes elevated objects to ground-only layouts.";

        /// <inheritdoc/>
        public override void Execute(bool value, MainWindow app) {
            if (app.Rendering) {
                Console.Error.WriteLine(string.Format(inProgress, "speaker virtualization"));
                app.IsEnabled = false;
                return;
            }

            Settings.Default.speakerVirtualizer = value;
        }
    }
}