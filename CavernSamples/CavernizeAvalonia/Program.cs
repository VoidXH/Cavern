using Avalonia;

namespace CavernizeAvalonia;

static class Program {
    public static string[] Args { get; private set; } = [];

    [STAThread]
    public static void Main(string[] args) {
        Args = args;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
