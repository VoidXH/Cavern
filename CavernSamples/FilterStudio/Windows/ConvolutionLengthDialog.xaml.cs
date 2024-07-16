using System;

using Cavern.WPF.BaseClasses;

namespace FilterStudio.Windows {
    /// <summary>
    /// Shows a filter length selector when converting a filter graph to convolution filters.
    /// </summary>
    public partial class ConvolutionLengthDialog : OkCancelDialog {
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
            Resources.MergedDictionaries.Add(Cavern.WPF.Consts.Language.GetCommonStrings());
            InitializeComponent();
        }
    }
}