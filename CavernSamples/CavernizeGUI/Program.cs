using Avalonia;
using VoidX.WPF.FFmpeg;

namespace CavernizeGUI {
    static class Program {
        public static string[] Args { get; private set; } = [];

        /// <summary>
        /// The application runs from a console. This makes FFmpeg output to the same console.
        /// </summary>
        public static bool ConsoleMode { get; private set; }

        public static int ExitCode { get; set; }

        /// <summary>
        /// Main entry point.
        /// </summary>
        [STAThread]
        public static int Main(string[] args) {
            Args = args;
            // Hide the console in Windows mode
            if (args.Length != 0 && (args.Length != 1 || !File.Exists(args[0]))) {
                ConsoleMode = true;
                FFmpeg.ConsoleMode = true;
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return ExitCode;
        }

        public static bool IsHelpCommand(string[] args) =>
            args.Any(arg => arg.Equals("-help", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-h", StringComparison.OrdinalIgnoreCase));

        public static AppBuilder BuildAvaloniaApp() => AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
