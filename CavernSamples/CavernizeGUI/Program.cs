using Avalonia;
using VoidX.WPF.FFmpeg;

namespace CavernizeGUI;

static class Program {
    public static string[] Args { get; private set; } = [];
    public static bool ConsoleMode { get; private set; }
    public static int ExitCode { get; set; }

    [STAThread]
    public static int Main(string[] args) {
        Args = args;
        ConsoleMode = IsConsoleMode(args);
        if (ConsoleMode) {
            FFmpeg.ConsoleMode = true;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return ExitCode;
    }

    static bool IsConsoleMode(string[] args) =>
        args.Length != 0 && (args.Length != 1 || !File.Exists(args[0]));

    public static bool IsHelpCommand(string[] args) =>
        args.Any(arg => arg.Equals("-help", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-h", StringComparison.OrdinalIgnoreCase));

    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
