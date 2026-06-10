using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace CavernizeGUI;

// Shutdown process and related debug features
partial class MainWindow {
    static void CheckBlocks() {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) {
            return;
        }

        foreach (Window window in desktop.Windows.Where(window => window.IsVisible)) {
            Debug.WriteLine("This window is still open: " + window.GetType().FullName);
        }
    }
}
