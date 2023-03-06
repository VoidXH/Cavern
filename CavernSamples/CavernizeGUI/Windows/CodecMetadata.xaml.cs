using System.Windows;

using Cavern.Format.Common;

namespace CavernizeGUI.Windows {
    /// <summary>
    /// Displays field-level debug information about the selected source track.
    /// </summary>
    public partial class CodecMetadata : Window {
        public CodecMetadata(ReadableMetadata source) {
            InitializeComponent();
            data.ItemsSource = source.Headers;
        }
    }
}