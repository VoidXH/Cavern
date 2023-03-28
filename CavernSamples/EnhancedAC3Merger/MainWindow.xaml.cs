using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Shell;

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
        /// Background merge performer and feedback provider.
        /// </summary>
        readonly TaskEngine runner;

        /// <summary>
        /// Main application window.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            CreateConsts();
            ffmpeg = new FFmpeg(null, Settings.Default.ffmpeg);
            TaskbarItemInfo = new TaskbarItemInfo {
                ProgressState = TaskbarItemProgressState.Normal
            };
            runner = new TaskEngine(progress, TaskbarItemInfo, null);
        }

        /// <summary>
        /// Search for FFmpeg's executable.
        /// </summary>
        void LocateFFmpeg(object _, RoutedEventArgs e) {
            ffmpeg.Locate();
            Settings.Default.ffmpeg = ffmpeg.Location;
        }

        /// <summary>
        /// Start merging the selected tracks.
        /// </summary>
        void Merge(object _, RoutedEventArgs e) {
            if (runner.IsOperationRunning) {
                Error("Another operation is already running.");
                return;
            }
            if (!ffmpeg.Found) {
                Error("FFmpeg wasn't found, please locate.");
                return;
            }

            InputChannel[][] streams = GetStreams();
            if (streams == null) {
                return;
            }

            SaveFileDialog saver = new SaveFileDialog {
                Filter = "E-AC-3 files|*.ec3"
            };
            if (saver.ShowDialog().Value) {
                runner.Run(() => Process(runner, streams, saver.FileName));
            }
        }

        /// <summary>
        /// Save the settings on exiting.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            Settings.Default.Save();
        }
    }
}