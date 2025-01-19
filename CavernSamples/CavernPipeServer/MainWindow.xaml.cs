using System;
using System.ComponentModel;
using System.Drawing;
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
        /// Right-click menu of the tray icon.
        /// </summary>
        readonly ContextMenuStrip contextMenu;

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
        /// Main status/configuration window and background operation handler.
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add((string)language["MOpen"], null, Open);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add((string)language["MRest"], null, Restart);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add((string)language["MExit"], null, Exit);

            icon = new NotifyIcon {
                Icon = new Icon(Application.GetResourceStream(new Uri("Resources/Icon.ico", UriKind.Relative)).Stream),
                Visible = true,
                ContextMenuStrip = contextMenu,
            };
            icon.DoubleClick += Open;

            meters = new ThreadSafeChannelMeters(canvas, chProto, vmProto);
            handler = new PipeHandler();
            handler.OnRenderingStarted += meters.Enable;
            handler.StatusChanged += OnServerStatusChange;
            handler.MetersAvailable += meters.Update;
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
        /// Show the configuration dialog.
        /// </summary>
        void Open(object _, EventArgs e) {
            Show();
            WindowState = WindowState.Normal;
        }

        /// <summary>
        /// Try to restart the pipe.
        /// </summary>
        void Restart(object _, EventArgs __) {
            try {
                handler.Start();
            } catch (InvalidOperationException e) {
                Consts.Language.ShowError(e.Message);
            }
        }

        /// <summary>
        /// Close the CavernPipe server.
        /// </summary>
        void Exit(object _, EventArgs e) {
            exiting = true;
            handler.Dispose();
            icon.Dispose();
            Application.Current.Shutdown();
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
    }
}