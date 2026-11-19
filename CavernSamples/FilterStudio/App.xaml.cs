using System.Windows;

using VoidX.WPF.Language;

using FilterStudio.Resources;

namespace FilterStudio {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        /// <summary>
        /// Set the language strings with the app launch.
        /// </summary>
        public App() {
            LanguageSettings.CultureOverride = Settings.Default.language;
            Resources.MergedDictionaries.Add(Consts.Language.GetMainWindowStrings());
        }
    }
}