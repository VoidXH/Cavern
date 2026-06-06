using Microsoft.Win32;
using System.IO;

namespace Cavern.WPF.Dialogs {
    /// <summary>
    /// Operations on <see cref="OpenFileDialog"/>s.
    /// </summary>
    public static class OpenFileDialogExtensions {
        /// <summary>
        /// Show the file picker, and handle potential errors.
        /// </summary>
        /// <param name="dialog">The dialog to show</param>
        /// <param name="onSelected">What action to perform when a file was selected</param>
        public static void ShowDialogSafe(this OpenFileDialog dialog, Action onSelected) {
            if (!Directory.Exists(dialog.InitialDirectory)) {
                dialog.InitialDirectory = null;
            }

            try {
                if (dialog.ShowDialog().Value) {
                    onSelected();
                }
            } catch (ArgumentException) {
                dialog.InitialDirectory = null;
                if (dialog.ShowDialog().Value) {
                    onSelected();
                }
            }
        }
    }
}
