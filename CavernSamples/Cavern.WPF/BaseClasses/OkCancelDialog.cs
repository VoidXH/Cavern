using System.Windows;

namespace Cavern.WPF.BaseClasses {
    /// <summary>
    /// A dialog with OK and Cancel buttons.
    /// </summary>
    public class OkCancelDialog : Window {
        /// <summary>
        /// Closes the dialog with the settings applied.
        /// </summary>
        protected virtual void OK(object _, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Closes the dialog with no change applied.
        /// </summary>
        protected void Cancel(object _, RoutedEventArgs e) => Close();
    }
}