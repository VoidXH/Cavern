using System.Windows;
using System.Windows.Media.Imaging;

namespace Cavern.WPF.Dialogs {
    /// <summary>
    /// Displays a single image.
    /// </summary>
    public partial class ImageDialog : Window {
        /// <summary>
        /// Displays a single image.
        /// </summary>
        /// <remarks>The <see cref="Window.Title"/>, <see cref="FrameworkElement.Width"/>, and <see cref="FrameworkElement.Height"/>
        /// should be set before showing the dialog.</remarks>
        public ImageDialog(BitmapSource image) {
            InitializeComponent();
            this.image.Source = image;
        }
    }
}
