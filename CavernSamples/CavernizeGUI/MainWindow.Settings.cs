using System.Windows.Controls;
using System.Windows;

using CavernizeGUI.Resources;

namespace CavernizeGUI {
    partial class MainWindow {
        /// <summary>
        /// Update the setting related to the surround swap feature when the toggle's state has changed.
        /// </summary>
        void SurroundSwapChange(object source, RoutedEventArgs _) => Settings.Default.surroundSwap = ((MenuItem)source).IsChecked;

        /// <summary>
        /// Update the setting related to the surround swap feature when the toggle's state has changed.
        /// </summary>
        void WAVChannelSkipChange(object source, RoutedEventArgs _) => Settings.Default.wavChannelSkip = ((MenuItem)source).IsChecked;
    }
}