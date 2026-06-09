using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace CavernizeGUI;

public partial class App : Application {
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            MainViewModel viewModel = new();
            if (Program.Args.Length != 0) {
                viewModel.InitializeCommandLine(Program.Args);
            }

            desktop.MainWindow = new MainWindow {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
