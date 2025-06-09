using System.Windows;

using Cavern.CavernSettings;

namespace CavernizeGUI.Windows {
    /// <summary>
    /// Configures a repository of <see cref="UpmixingSettings"/>.
    /// </summary>
    public partial class UpmixingSetup : Window {
        /// <summary>
        /// Settings to change if the user clicks "OK" in this window.
        /// </summary>
        readonly UpmixingSettings settings;

        /// <summary>
        /// Configures a repository of upmixing <paramref name="settings"/>.
        /// </summary>
        public UpmixingSetup(UpmixingSettings settings) {
            InitializeComponent();
            this.settings = settings;
            matrixUpmix.IsChecked = settings.MatrixUpmixing;
            cavernize.IsChecked = settings.Cavernize;
            effect.Value = settings.Effect * 100;
            smoothness.Value = settings.Smoothness * 100;
        }

        /// <summary>
        /// Restore the default values.
        /// </summary>
        void Reset(object _, RoutedEventArgs e) {
            matrixUpmix.IsChecked = false;
            cavernize.IsChecked = false;
            effect.Value = 75;
            smoothness.Value = 80;
        }

        /// <summary>
        /// Close the window while saving the settings.
        /// </summary>
        void Ok(object _, RoutedEventArgs e) {
            settings.MatrixUpmixing = matrixUpmix.IsChecked.Value;
            settings.Cavernize = cavernize.IsChecked.Value;
            settings.Effect = (float)(effect.Value * .01f);
            settings.Smoothness = (float)(smoothness.Value * .01f);
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Close the window while not saving the settings.
        /// </summary>
        void Cancel(object _, RoutedEventArgs e) => Close();
    }
}