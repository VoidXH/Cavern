using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;

namespace CavernPipeServer {
    /// <summary>
    /// Main status/configuration window and background operation handler.
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// CavernPipe tray icon handle.
        /// </summary>
        readonly NotifyIcon icon;

        /// <summary>
        /// Network connection with watchdog.
        /// </summary>
        readonly PipeHandler handler;

        /// <summary>
        /// Displays real-time renderer channel gains.
        /// </summary>
        readonly ThreadSafeChannelMeters meters;

        /// <summary>
        /// Language string access.
        /// </summary>
        readonly ResourceDictionary language = Consts.Language.GetMainWindowStrings();

        /// <summary>
        /// Closing the application is in progress.
        /// </summary>
        bool exiting;

        /// <summary>
        /// The issue with the last tried playback.
        /// </summary>
        string lastError;

        /// <summary>
        /// Main status/configuration window and background operation handler.
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            icon = new NotifyIcon {
                Icon = new Icon(Application.GetResourceStream(new Uri("Resources/Icon.ico", UriKind.Relative)).Stream),
                Visible = true,
                ContextMenuStrip = CreateContextMenu(),
            };
            icon.DoubleClick += Open;

            meters = new ThreadSafeChannelMeters(canvas, chProto, vmProto);
            handler = new PipeHandler();
            handler.OnRenderingStarted += OnRenderingStarted;
            handler.StatusChanged += OnServerStatusChange;
            handler.MetersAvailable += meters.Update;
            handler.OnException += StoreErrorMessage;

            OnServerStatusChange();
        }

        /// <summary>
        /// Change the closing event to going back to the system tray. <see cref="Exit"/> is a <see cref="contextMenu"/> option.
        /// </summary>
        protected override void OnClosing(CancelEventArgs e) {
            Hide();
            e.Cancel = true;
        }

        /// <summary>
        /// Prepare the UI for handling data coming from networking/rendering threads.
        /// </summary>
        void OnRenderingStarted() {
            meters.Enable();
            lastError = null;
        }

        /// <summary>
        /// Update the UI to reflect the server's status.
        /// </summary>
        void OnServerStatusChange() {
            if (exiting) {
                return;
            }

            canvas.Dispatcher.Invoke(() => {
                Color statusColor;
                if (handler.IsConnected) {
                    statusColor = Color.FromArgb(255, 0, 147, 191);
                    status.Content = (string)language["SProc"];
                } else if (handler.Running) {
                    statusColor = Color.FromArgb(255, 0, 255, 0);
                    status.Content = (string)language["SWait"];
                } else {
                    statusColor = Color.FromArgb(255, 255, 0, 0);
                    status.Content = (string)language["SNoSv"];
                }
                status.Background = new SolidColorBrush(statusColor);
            });

            if (!handler.IsConnected) {
                meters.Disable();
            }
        }

        /// <summary>
        /// Handle exceptions that result in a stopped playback.
        /// </summary>
        void StoreErrorMessage(Exception e) {
            if (e.GetType() == typeof(IOException) && e.Source == "System.IO.Pipes") {
                lastError = (string)language["NDisc"];
                return;
            } else if (e.StackTrace.Contains("QueueStream")) {
                return; // Rendering threads are stopped by setting them to null (yeah...)
            } else {
                lastError = e.ToString();
            }
        }
    }
}