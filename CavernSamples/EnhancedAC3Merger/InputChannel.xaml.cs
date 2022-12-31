using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

using Cavern.Format;
using Cavern.Remapping;

using VoidX.WPF;

namespace EnhancedAC3Merger {
    /// <summary>
    /// Single channel input mapper control. Loads a file and selects one of its channels as an output channel.
    /// </summary>
    public partial class InputChannel : UserControl {
        /// <summary>
        /// This input channel is assigned.
        /// </summary>
        public bool Active => SelectedFile != null;

        /// <summary>
        /// The output channel to set with the control.
        /// </summary>
        public ReferenceChannel TargetChannel {
            get => targetChannel;
            set {
                targetChannel = value;
                channelName.Text = EnumToTitleCase.GetTitleCase(value.ToString());
            }
        }
        ReferenceChannel targetChannel;

        /// <summary>
        /// The file selected for this channel.
        /// </summary>
        public string SelectedFile { get; private set; }

        /// <summary>
        /// Index of the selected channel in the <see cref="SelectedFile"/>.
        /// </summary>
        public int SelectedChannel => channelIndex.SelectedIndex;

        /// <summary>
        /// Single channel input mapper control.
        /// </summary>
        public InputChannel() => InitializeComponent();

        /// <summary>
        /// Opens a file for selecting a channel from.
        /// </summary>
        void OpenFile(object _, RoutedEventArgs e) {
            OpenFileDialog opener = new OpenFileDialog() {
                Filter = "Supported input files|" + AudioReader.filter
            };
            if (opener.ShowDialog().Value) {
                AudioReader reader;
                try {
                    reader = AudioReader.Open(opener.FileName);
                    reader.ReadHeader();
                } catch (Exception ex) {
                    MessageBox.Show("Importing the file failed for the following reason: " + ex.Message);
                    return;
                }

                SelectedFile = opener.FileName;
                channelIndex.ItemsSource = reader.GetRenderer().GetChannels();
                channelIndex.SelectedIndex = 0;
                reader.Dispose();
            }
        }

        /// <summary>
        /// Display the referenced channel on string conversion.
        /// </summary>
        public override string ToString() => channelName.Text;
    }
}