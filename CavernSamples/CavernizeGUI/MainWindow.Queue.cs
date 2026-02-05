using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Windows;

using Cavern.Format.Common;

using Cavernize.Logic.Models;
using Cavernize.Logic.Rendering;
using CavernizeGUI.Resources;

namespace CavernizeGUI {
    partial class MainWindow {
        /// <summary>
        /// Queue a rendering process.
        /// </summary>
        void Queue(object _, RoutedEventArgs e) {
            Action renderTask = GetRenderTask();
            if (renderTask != null) {
                if (Width < minWidth) {
                    Width = minWidth;
                }
                jobs.Add(new QueuedJob(LoadedFile, (CavernizeTrack)tracks.SelectedItem, RenderTarget, (ExportFormat)audio.SelectedItem, renderTask));
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
                jobs.RemoveAt(queuedJobs.SelectedIndex);
            }
        }

        /// <summary>
        /// Start processing the queue.
        /// </summary>
        void StartQueue(object _, RoutedEventArgs e) {
            QueuedJob[] jobsToRun = [.. jobs];
            taskEngine.Run(() => QueueRunnerTask(jobsToRun), Error);
        }

        /// <summary>
        /// Handle when files are dropped on the list of queued jobs.
        /// </summary>
        void QueueDrop(object _, DragEventArgs e) {
            if (e.Data is DataObject obj && obj.ContainsFileDropList()) {
                AudioFile oldFile = LoadedFile;
                StringCollection files = obj.GetFileDropList();
                List<string> invalids = [];
                if (files.Count > 1 &&
                    MessageBox.Show((string)language["QuAll"], (string)language["QuAlT"], MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                    ProcessDroppedFilesAtOnce(files, invalids);
                } else {
                    ProcessDroppedFilesSeparately(files, invalids);
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
            if (!folder.ShowDialog().Value) {
                return;
            }

            for (int i = 0, c = files.Count; i < c; i++) {
                try {
                    OpenContent(files[i]);
                } catch {
                    invalids.Add(Path.GetFileName(files[i]));
                    continue;
                }

                CavernizeTrack target = (CavernizeTrack)tracks.SelectedItem;
                Codec codec = ((ExportFormat)audio.SelectedItem).Codec;
                string container = MergeToContainer.GetPossibleContainers(target, codec, ffmpeg);
                container = container.Substring(container.IndexOf('|') + 2, 4);
                string outputPath = Path.Combine(folder.FolderName, Path.GetFileNameWithoutExtension(files[i])) + container;
                QueueRenderTask(GetRenderTask(outputPath), invalids, Path.GetFileName(files[i]));
            }
        }

        /// <summary>
        /// Add files to the queue, prompting the user to select a distinct output path for all of them.
        /// </summary>
        void ProcessDroppedFilesSeparately(StringCollection files, List<string> invalids) {
            for (int i = 0, c = files.Count; i < c; i++) {
                try {
                    OpenContent(files[i]);
                } catch {
                    invalids.Add(Path.GetFileName(files[i]));
                    continue;
                }

                QueueRenderTask(GetRenderTask(), invalids, Path.GetFileName(files[i]));
            }
        }

        /// <summary>
        /// Add the currently loaded track to the queue, or add its <paramref name="fileName"/> to the list of
        /// <paramref name="invalids"/> if rendering is not possible for any reason.
        /// </summary>
        void QueueRenderTask(Action renderTask, List<string> invalids, string fileName) {
            if (renderTask != null) {
                jobs.Add(new QueuedJob(LoadedFile, (CavernizeTrack)tracks.SelectedItem, RenderTarget, (ExportFormat)audio.SelectedItem, renderTask));
            } else {
                invalids.Add(fileName);
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
                Dispatcher.Invoke(() => this.jobs.Remove(job));
            }
            Dispatcher.Invoke(() => queuedJobs.AllowDrop = true);
        }
    }
}
