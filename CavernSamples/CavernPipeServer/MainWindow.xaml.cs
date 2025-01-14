using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

using Application = System.Windows.Application;

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
        readonly PipeHandler handler = new PipeHandler();

        /// <summary>
        /// Main status/configuration window and background operation handler.
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open", null, Open);
            contextMenu.Items.Add("Exit", null, Exit);

            icon = new NotifyIcon {
                Icon = new Icon(Application.GetResourceStream(new Uri("Resources/Icon.ico", UriKind.Relative)).Stream),
                Visible = true,
                ContextMenuStrip = contextMenu,
            };
            icon.DoubleClick += Open;
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
        void Open(object sender, EventArgs e) {
            Show();
            WindowState = WindowState.Normal;
        }

        /// <summary>
        /// Close the CavernPipe server.
        /// </summary>
        void Exit(object sender, EventArgs e) {
            handler.Dispose();
            icon.Dispose();
            Application.Current.Shutdown();
        }
    }
}