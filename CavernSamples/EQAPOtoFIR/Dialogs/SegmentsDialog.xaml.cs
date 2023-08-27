using System.Windows;

namespace EQAPOtoFIR.Dialogs {
    /// <summary>
    /// Segment count selector dialog.
    /// </summary>
    public partial class SegmentsDialog : Window {
        /// <summary>
        /// Resulting segment count.
        /// </summary>
        public int Segments => segments.Value;

        /// <summary>
        /// Segment count selector dialog.
        /// </summary>
        public SegmentsDialog() => InitializeComponent();

        void OK(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}