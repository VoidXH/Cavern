using System;
using System.IO;
using System.Windows;

using CavernizeGUI.CommandLine;

namespace CavernizeGUI {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        void Main(object _, StartupEventArgs e) {
            MainWindow app = new MainWindow();
            string[] args = e.Args;
            if (args.Length != 0) {
                int start = 0;
                if (File.Exists(args[0])) {
                    app.OpenContent(args[0]);
                    start = 1;
                }
                for (int i = start; i < args.Length;) {
                    Command command = Command.GetCommandByArgument(args[i]);
                    if (command == null) {
                        MessageBox.Show($"Invalid command ({args[i]}), try using -help.");
                        break;
                    } else {
                        if (i + command.Parameters >= args.Length) {
                            MessageBox.Show($"Too few arguments for {args[i]}.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        }

                        try {
                            command.Execute(args, ++i, app);
                        } catch (Exception exception) {
                            MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        }
                        i += command.Parameters;
                    }
                }
            }
            app.Show();
        }
    }
}