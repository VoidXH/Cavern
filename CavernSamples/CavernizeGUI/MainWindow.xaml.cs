using Cavern;
using Cavern.Format;
using Cavern.Remapping;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using VoidX.WPF;

namespace CavernizeGUI {
    public partial class MainWindow : Window {
        /// <summary>
        /// Playback environment used for rendering.
        /// </summary>
        readonly Listener listener;

        AudioReader reader;
        readonly TaskEngine taskEngine;

        public MainWindow() {
            InitializeComponent();
            listener = new(); // Create a listener, which triggers the loading of saved environment settings
            layout.Text = "Loaded layout: " + Listener.GetLayoutName();
            savePath.Text = "";
            taskEngine = new(progress, status);
        }

        /// <summary>
        /// Open file button event; loads a WAV file to <see cref="reader"/>.
        /// </summary>
        void OpenFile(object _, RoutedEventArgs e) {
            OpenFileDialog dialog = new() {
                Filter = "RIFF WAVE files (*.wav)|*.wav"
            };
            if (dialog.ShowDialog().Value) {
                reader = new RIFFWaveReader(dialog.FileName);
                fileName.Text = Path.GetFileName(dialog.FileName);
            }
        }

        void RenderTask() {
            listener.SampleRate = reader.SampleRate;
            // TODO: move Cavernize from Unity to Cavern and do this
        }

        void Render(object _, RoutedEventArgs e) {
            if (reader != null)
                taskEngine.Run(RenderTask);
        }
    }
}