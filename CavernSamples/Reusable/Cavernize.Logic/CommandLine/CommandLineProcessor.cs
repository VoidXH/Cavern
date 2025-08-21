using Cavernize.Logic.CommandLine.BaseClasses;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine;

/// <summary>
/// Handles command line argument parsing for Cavernize implementations.
/// </summary>
public static class CommandLineProcessor {
    /// <summary>
    /// Prepares a Cavernize application for a conversion based on command line setup.
    /// </summary>
    public static bool Initialize(string[] args, ICavernizeApp app) {
        if (args.Length == 0) {
            Console.Error.WriteLine("No command line arguments provided. Use -help to list the commands.");
            return false;
        }

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
                Console.Error.WriteLine($"Invalid command ({args[i]}), try using -help.");
                return false;
            } else {
                if (i + command.Parameters >= args.Length) {
                    Console.Error.WriteLine($"Too few arguments for {args[i]}.");
                    return false;
                }

                try {
                    command.Execute(args, ++i, app);
                } catch (Exception exception) {
                    if (exception is not CommandProcessingCanceledException) {
                        if (string.IsNullOrEmpty(exception.Message)) {
                            Console.Error.WriteLine(exception);
                        } else {
                            Console.Error.WriteLine(exception.Message);
                        }
                    }
                    return false;
                }
                i += command.Parameters;
            }
        }
        return true;
    }
}
