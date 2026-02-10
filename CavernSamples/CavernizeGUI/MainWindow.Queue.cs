using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;

using Cavernize.Logic.Models;
using Cavernize.Logic.Utilities;
using CavernizeGUI.Resources;

namespace CavernizeGUI {
    partial class MainWindow {
        /// <summary>
        /// Queue a rendering process.
        /// </summary>
        void Queue(object _, RoutedEventArgs e) {
            if (queue.AddCurrent(null) && Width < minWidth) {
                Width = minWidth;
            }
        }

        /// <summary>
        /// Removes a queued job.
        /// </summary>
        void RemoveQueued(object _, RoutedEventArgs e) {
            if (taskEngine.IsOperationRunning) {
                Error((string)language["ReQOp"]);
            } else if (queuedJobs.SelectedItem == null) {
                Error((string)language["ReQSe"]);
            } else {
                queue.Jobs.RemoveAt(queuedJobs.SelectedIndex);
            }
        }

        /// <summary>
        /// Start processing the queue.
        /// </summary>
        void StartQueue(object _, RoutedEventArgs e) {
            QueuedJob[] jobsToRun = [.. queue.Jobs];
            taskEngine.Run(() => QueueRunnerTask(jobsToRun), Error);
        }

        /// <summary>
        /// Handle when files are dropped on the list of queued jobs.
        /// </summary>
        void QueueDrop(object _, DragEventArgs e) {
            if (e.Data is DataObject obj && obj.ContainsFileDropList()) {
                AudioFile oldFile = LoadedFile;
                StringCollection files = obj.GetFileDropList();
                files.FlattenPaths();
                List<string> invalids = [];
                if (files.Count > 1 &&
                    MessageBox.Show((string)language["QuAll"], (string)language["QuAlT"], MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                    ProcessDroppedFilesAtOnce(files, invalids);
                } else {
                    queue.AddRange(files, invalids);
                }
                LoadedFile = oldFile;
                if (invalids.Count != 0) {
                    Error($"{(string)language["DropI"]}\n{string.Join('\n', invalids)}");
                }
            }
        }

        /// <summary>
        /// Add files to the queue, prompting the user to select a single folder where all output files will be written
        /// in the current configuration's default container.
        /// </summary>
        void ProcessDroppedFilesAtOnce(StringCollection files, List<string> invalids) {
            OpenFolderDialog folder = new OpenFolderDialog {
                InitialDirectory = Settings.Default.lastDirectory
            };
            if (folder.ShowDialog().Value) {
                queue.AddRange(files, folder.FolderName, invalids, ffmpeg);
            }
        }

        /// <summary>
        /// Run all queued jobs one after another.
        /// </summary>
        void QueueRunnerTask(QueuedJob[] jobs) {
            Dispatcher.Invoke(() => queuedJobs.AllowDrop = false);
            for (int i = 0; i < jobs.Length; i++) {
                QueuedJob job = jobs[i];
                Dispatcher.Invoke(() => {
                    job.Prepare(this);
                    AttachToListener();
                });
                job.Run();
                Dispatcher.Invoke(() => queue.Jobs.Remove(job));
            }
            Dispatcher.Invoke(() => {
                queuedJobs.AllowDrop = true;
                Reset();
            });
        }
    }
}
