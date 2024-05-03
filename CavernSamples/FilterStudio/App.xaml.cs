using System.Windows;

namespace FilterStudio {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        /// <summary>
        /// Set the language strings with the app launch.
        /// </summary>
        public App() => Resources.MergedDictionaries.Add(Consts.Language.GetMainWindowStrings());
    }
}