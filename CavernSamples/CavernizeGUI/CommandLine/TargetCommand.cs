using System;

using CavernizeGUI.Elements;

namespace CavernizeGUI.CommandLine {
    /// <summary>
    /// Selects a standard channel layout.
    /// </summary>
    class TargetCommand : Command {
        /// <summary>
        /// Full name of the command, including a preceding character like '-' if exists.
        /// </summary>
        public override string Name => "-target";

        /// <summary>
        /// Shorthand for <see cref="Name"/>.
        /// </summary>
        public override string Alias => "-t";

        /// <summary>
        /// Number of parameters this command will use.
        /// </summary>
        public override int Parameters => 1;

        /// <summary>
        /// Description of the command that is displayed in the command list (help).
        /// </summary>
        public override string Help => "Selects a standard channel layout (render target).";

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="args">List of all calling arguments for the software</param>
        /// <param name="offset">The index of the first argument that is a parameter of this command</param>
        /// <param name="app">Reference to the main window of the application - operations should be performed though the UI</param>
        public override void Execute(string[] args, int offset, MainWindow app) {
            if (app.Rendering) {
                Console.Error.WriteLine(string.Format(inProgress, "render target"));
                app.IsEnabled = false;
                return;
            }

            for (int i = RenderTarget.Targets.Length - 1; i >= 0; i--) { // Sides before fronts
                string name = RenderTarget.Targets[i].Name;
                if (args[offset].Equals(name)) {
                    app.renderTarget.SelectedItem = RenderTarget.Targets[i];
                    return;
                }

                int index = name.IndexOf(' ');
                if (index != -1) {
                    name = name[..index];
                    if (args[offset].Equals(name)) {
                        app.renderTarget.SelectedItem = RenderTarget.Targets[i];
                        return;
                    }
                }

                name = name.Replace(".", string.Empty);
                if (args[offset].Equals(name)) {
                    app.renderTarget.SelectedItem = RenderTarget.Targets[i];
                    return;
                }
            }

            string valids = string.Join<RenderTarget>(", ", RenderTarget.Targets);
            throw new CommandException($"Invalid rendering target ({args[offset]}). Valid options are: {valids}.");
        }
    }
}