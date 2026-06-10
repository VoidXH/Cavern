using Avalonia.Controls;

namespace CavernizeGUI;

public partial class MainWindow : Window {
    public MainWindow() => InitializeComponent();

    MainViewModel ViewModel => (MainViewModel)DataContext;

    protected override void OnOpened(EventArgs e) {
        base.OnOpened(e);
        ApplyViewScale();
        BuildNativeMenu();
        BuildWindowsMenu();
        _ = CheckForUpdates();
    }

    protected override void OnClosed(EventArgs e) {
        ViewModel.Dispose();
        CheckBlocks();
        base.OnClosed(e);
    }
}
