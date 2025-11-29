using System.Windows;

namespace CavernizeGUI {
    // Shutdown process and related debug features
    partial class MainWindow {
        static void CheckBlocks() {
#if DEBUG
            foreach (Window window in Application.Current.Windows) {
                Error("This window is still open: " + window.GetType().FullName);
            }
#endif
        }
    }
}
