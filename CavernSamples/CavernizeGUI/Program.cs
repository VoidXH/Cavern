using System;
using System.IO;
using System.Runtime.InteropServices;

using VoidX.WPF;

namespace CavernizeGUI {
    class Program {
        /// <summary>
        /// The application runs from a console. This makes FFmpeg output to the same console.
        /// </summary>
        public static bool ConsoleMode { get; private set; }

        /// <summary>
        /// Main entry point.
        /// </summary>
        [STAThread]
        public static void Main(string[] args) {
            // Hide the console in Windows mode
            if (args.Length == 0 || (args.Length == 1 && File.Exists(args[0]))) {
                ShowWindow(GetConsoleWindow(), 0);
            } else {
                ConsoleMode = true;
                FFmpeg.ConsoleMode = true;
            }

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        /// <summary>
        /// Get the handle for the application's main console window.
        /// </summary>
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        /// <summary>
        /// Show or hide a window in the system.
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}