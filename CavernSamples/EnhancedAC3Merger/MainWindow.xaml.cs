using System.Windows;

namespace EnhancedAC3Merger {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() => InitializeComponent();

        /// <summary>
        /// Start merging the selected tracks.
        /// </summary>
        void Merge(object _, RoutedEventArgs e) {
            // TODO: check for a 15-channel FBW limit
            // TODO: load the files, select the tracks, combine them to WAVs in good order, send them through FFmpeg, then the merger
        }
    }
}