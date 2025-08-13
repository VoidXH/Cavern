using System;
using System.Diagnostics;
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

        /// <summary>
        /// Set application language to English.
        /// </summary>
        void LanguageEnglish(object _, RoutedEventArgs __) => SetLanguage("en-US");

        /// <summary>
        /// Set application language to Hungarian.
        /// </summary>
        void LanguageHungarian(object _, RoutedEventArgs __) => SetLanguage("hu-HU");

        /// <summary>
        /// Set application language to an invalid, untranslated one.
        /// </summary>
        void LanguageTest(object _, RoutedEventArgs __) => SetLanguage("te-ST");

        /// <summary>
        /// Overwrite the autodetected language.
        /// </summary>
        /// <param name="code">Standard language code</param>
        void SetLanguage(string code) {
            Settings.Default.language = code;
            Restart();
        }

        /// <summary>
        /// Relaunch the application when needed.
        /// </summary>
        void Restart() {
            if (Rendering) {
                Error((string)language["OpRes"]);
                return;
            } else {
                string path = Environment.ProcessPath;
                Process.Start(path);
                Application.Current.Shutdown();
            }
        }
    }
}