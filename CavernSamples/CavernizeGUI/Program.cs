using Avalonia;
using Cavernize.Logic.CommandLine;
using VoidX.WPF.FFmpeg;

namespace CavernizeGUI;

static class Program {
    public static string[] Args { get; private set; } = [];
    public static bool ConsoleMode { get; private set; }

    [STAThread]
    public static int Main(string[] args) {
        Args = args;
        ConsoleMode = IsConsoleMode(args);
        if (ConsoleMode) {
            FFmpeg.ConsoleMode = true;
            using CavernizeSession session = new();
            session.StatusChanged += Console.WriteLine;
            session.WarningRaised += text => Console.Error.WriteLine(text);
            return CommandLineProcessor.Initialize(args, session) || IsHelpCommand(args) ? 0 : 1;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    static bool IsConsoleMode(string[] args) =>
        args.Length != 0 && (args.Length != 1 || !File.Exists(args[0]));

    static bool IsHelpCommand(string[] args) =>
        args.Any(arg => arg.Equals("-help", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-h", StringComparison.OrdinalIgnoreCase));

    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
