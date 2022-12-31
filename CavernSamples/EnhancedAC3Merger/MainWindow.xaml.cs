using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Cavern.Format;

using VoidX.WPF;

namespace EnhancedAC3Merger {
    /// <summary>
    /// Main application window.
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// Handles FFmpeg location and launch.
        /// </summary>
        readonly FFmpeg ffmpeg;

        /// <summary>
        /// All possible input tracks, even if they're not assigned.
        /// </summary>
        readonly InputChannel[] inputs;

        /// <summary>
        /// Main application window.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            ffmpeg = new FFmpeg(null, null);
            inputs = new InputChannel[] {
                fl, fr, fc, lfe, sl, sr, // Bed order doesn't matter, it's handled by FFmpeg
                flc, frc, rl, rr, rc, gv, wl, wr, tfl, tfr, tfc, tsl, tsr // Others are in E-AC-3 channel assignment order
            };
        }

        /// <summary>
        /// Search for FFmpeg's executable.
        /// </summary>
        void LocateFFmpeg(object _, RoutedEventArgs e) => ffmpeg.Locate();

        /// <summary>
        /// Start merging the selected tracks.
        /// </summary>
        void Merge(object _, RoutedEventArgs e) {
            if (!ffmpeg.Found) {
                Error("FFmpeg wasn't found, please locate.");
            }
            if (inputs.Count(x => x.Active) > 15) {
                Error("E-AC-3 can only contain 15 full bandwidth channels.");
            }
            InputChannel[] bedChannels = GetBed();
            if (bedChannels == null) {
                Error("Invalid bed layout. Only 2.0, 4.0, 5.0, and 5.1 are allowed.");
            }

            SaveFileDialog saver = new SaveFileDialog() {
                Filter = "E-AC-3 files|*.ec3"
            };
            if (!saver.ShowDialog().Value) {
                return;
            }

            AudioReader[] files = GetFiles();
            Dictionary<InputChannel, int> fileMap = Associate(files);
            InputChannel[][] streams = GetSubstreams(bedChannels);
            // TODO: write the streams while updating all files, FFmpeg encoding to E-AC-3, and then merge those streams

            for (int i = 0; i < files.Length; i++) {
                files[i].Dispose();
            }
        }
    }
}