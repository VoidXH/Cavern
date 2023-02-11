using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

namespace VoidX.WPF {
    /// <summary>
    /// Progress reporter and job handler.
    /// </summary>
    public class TaskEngine : IDisposable {
        static readonly TimeSpan lazyStatusDelta = new TimeSpan(0, 0, 1);

        readonly ProgressBar progressBar;
        readonly TaskbarItemInfo taskbar;
        readonly TextBlock progressLabel;

        Task operation;
        DateTime lastLazyStatus = DateTime.MinValue;

        /// <summary>
        /// A task is running and is not completed or failed.
        /// </summary>
        public bool IsOperationRunning => operation != null && operation.Status == TaskStatus.Running;

        /// <summary>
        /// Set the progress bar and status label to enable progress reporting on the UI.
        /// </summary>
        public TaskEngine(ProgressBar progressBar, TaskbarItemInfo taskbar, TextBlock progressLabel) {
            this.progressBar = progressBar;
            this.taskbar = taskbar;
            this.progressLabel = progressLabel;
        }

        /// <summary>
        /// Set the progress on the progress bar if it's set.
        /// </summary>
        public void UpdateProgressBar(double progress) {
            taskbar?.Dispatcher.Invoke(() => {
                if (progress < 1) {
                    taskbar.ProgressValue = progress;
                } else {
                    taskbar.ProgressValue = 0;
                }
            });
            progressBar?.Dispatcher.Invoke(() => {
                progressBar.Value = progress;
            });
        }

        /// <summary>
        /// Set the status text label, if it's given.
        /// </summary>
        public void UpdateStatus(string text) {
            progressLabel?.Dispatcher.Invoke(() => progressLabel.Text = text);
        }

        /// <summary>
        /// Set the status text label, if it's given. The label is only updated if
        /// the last update was <see cref="lazyStatusDelta"/> ago.
        /// </summary>
        public void UpdateStatusLazy(string text) {
            DateTime now = DateTime.Now;
            if (now - lastLazyStatus > lazyStatusDelta && progressLabel != null) {
                progressLabel.Dispatcher.Invoke(() => progressLabel.Text = text);
                lastLazyStatus = now;
            }
        }

        /// <summary>
        /// Run a new task if no task is running.
        /// </summary>
        public bool Run(Action task) {
            if (IsOperationRunning) {
                MessageBox.Show("Another operation is already running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            operation?.Dispose();
            operation = new Task(task);
            operation.Start();
            return true;
        }

        /// <summary>
        /// Free up resources.
        /// </summary>
        public void Dispose() {
            try {
                operation?.Dispose();
            } catch {
                // There are cases, like the open file dialog, where cancellation is not possible. The OS will free it anyway.
            }
            GC.SuppressFinalize(this);
        }
    }
}