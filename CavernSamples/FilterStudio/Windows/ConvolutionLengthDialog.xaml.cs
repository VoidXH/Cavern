using System;
using System.Windows;

namespace FilterStudio.Windows {
    /// <summary>
    /// Shows a filter length selector when converting a filter graph to convolution filters.
    /// </summary>
    public partial class ConvolutionLengthDialog : Window {
        /// <summary>
        /// The user-selected number of convolution samples per filter.
        /// </summary>
        public int Size => size.Value;

        /// <summary>
        /// Shows a filter length selector when converting a filter graph to convolution filters.
        /// </summary>
        public ConvolutionLengthDialog() {
            Resources.MergedDictionaries.Add(new() {
                Source = new Uri($";component/Resources/Styles.xaml", UriKind.RelativeOrAbsolute)
            });
            Resources.MergedDictionaries.Add(Consts.Language.GetConvolutionLengthDialogStrings());
            InitializeComponent();
        }

        /// <summary>
        /// Closes the dialog with the filter selected.
        /// </summary>
        void OK(object _, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Closes the dialog with no filter selected.
        /// </summary>
        void Cancel(object _, RoutedEventArgs e) => Close();
    }
}