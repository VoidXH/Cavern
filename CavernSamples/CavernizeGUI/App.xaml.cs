using System;
using System.IO;
using System.Windows;

using CavernizeGUI.CommandLine;

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
            if (args.Length != 0) {
                int start = 0;
                if (File.Exists(args[0])) {
                    try {
                        app.OpenContent(args[0]);
                    } catch (Exception ex) {
                        Console.Error.WriteLine(ex.Message);
                    }
                    start = 1;
                }
                for (int i = start; i < args.Length;) {
                    Command command = Command.GetCommandByArgument(args[i]);
                    if (command == null) {
                        Console.WriteLine($"Invalid command ({args[i]}), try using -help.");
                        app.IsEnabled = false;
                        break;
                    } else {
                        if (i + command.Parameters >= args.Length) {
                            Console.WriteLine($"Too few arguments for {args[i]}.");
                            app.IsEnabled = false;
                            break;
                        }

                        try {
                            command.Execute(args, ++i, app);
                        } catch (Exception exception) {
                            Console.Error.WriteLine(exception.Message);
                            app.IsEnabled = false;
                            break;
                        }
                        i += command.Parameters;
                    }
                }
            }

            if (app.IsEnabled) { // Marks to continue with launch or not - commands might disable it
                app.Show();
            } else {
                app.Close();
            }
        }
    }
}