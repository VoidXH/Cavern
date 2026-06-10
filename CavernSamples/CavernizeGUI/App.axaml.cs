using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace CavernizeGUI;

public partial class App : Application {
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            MainWindow window = new MainWindow();
            if (Program.Args.Length == 0) {
                desktop.MainWindow = window;
            } else {
                bool initialized = window.InitializeCommandLine(Program.Args);
                if (Program.ConsoleMode || !initialized) {
                    Program.ExitCode = initialized || Program.IsHelpCommand(Program.Args) ? 0 : 1;
                    Dispatcher.UIThread.Post(() => desktop.Shutdown(Program.ExitCode));
                    return;
                }
                desktop.MainWindow = window;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
