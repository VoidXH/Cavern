using System;
using System.IO;

using VoidX.WPF.FFmpeg;
using VoidX.WPF.Language;

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
            LanguageSettings.CultureOverride = Settings.Default.language;
            app.Resources.MergedDictionaries.Add(Consts.Language.GetMainWindowStrings());
            app.Resources.MergedDictionaries.Add(Consts.Language.GetRenderTargetSelectorStrings());
            try {
                app.InitializeComponent();
            } catch {
                Console.WriteLine((string)Consts.Language.GetMainWindowStrings()["dnErr"]);
                Console.ReadKey();
            }
            app.Run();
        }
    }
}
