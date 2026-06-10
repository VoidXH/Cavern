using System.Windows;

using Cavernize.Logic.CommandLine;

namespace CavernizeGUI {
    /// <summary>
    /// Windows application handler.
    /// </summary>
    public partial class App : Application {
        /// <summary>
        /// Windows application entry point that also handles console commands.
        /// </summary>
        void Main(object _, StartupEventArgs e) {
            MainWindow app = new MainWindow();
            string[] args = e.Args;
            if (args.Length == 0 || CommandLineProcessor.Initialize(args, app)) {
                app.Show();
            } else {
                app.Close();
            }
        }
    }
}
