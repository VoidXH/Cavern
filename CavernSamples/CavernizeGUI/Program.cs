using System;
using System.IO;
using System.Runtime.InteropServices;

using VoidX.WPF.FFmpeg;

using CavernizeGUI.Resources;

namespace CavernizeGUI {
    static class Program {
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
            if (args.Length != 0 && (args.Length != 1 || !File.Exists(args[0]))) {
                ConsoleMode = true;
                FFmpeg.ConsoleMode = true;
            }
            App app = new App();
            app.Resources.MergedDictionaries.Add(Consts.Language.GetMainWindowStrings());
            app.Resources.MergedDictionaries.Add(Consts.Language.GetRenderTargetSelectorStrings());
            Cavern.WPF.Consts.Language.Override = Settings.Default.language;
            try {
                app.InitializeComponent();
                if (!ConsoleMode) {
                    FreeConsole();
                }
            } catch {
                Console.WriteLine((string)Consts.Language.GetMainWindowStrings()["dnErr"]);
                Console.ReadKey();
            }
            app.Run();
        }

        /// <summary>
        /// Close the console window.
        /// </summary>
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();
    }
}