using System.Windows;

using CavernizeGUI.Resources;

namespace CavernizeGUI.Windows {
    /// <summary>
    /// Interaction logic for UpmixingSetup.xaml
    /// </summary>
    public partial class UpmixingSetup : Window {
        public UpmixingSetup() {
            InitializeComponent();
            matrixUpmix.IsChecked = UpmixingSettings.Default.MatrixUpmix;
            cavernize.IsChecked = UpmixingSettings.Default.Cavernize;
            effect.Value = UpmixingSettings.Default.Effect * 100;
            smoothness.Value = UpmixingSettings.Default.Smoothness * 100;
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
            UpmixingSettings.Default.MatrixUpmix = matrixUpmix.IsChecked.Value;
            UpmixingSettings.Default.Cavernize = cavernize.IsChecked.Value;
            UpmixingSettings.Default.Effect = (float)(effect.Value * .01f);
            UpmixingSettings.Default.Smoothness = (float)(smoothness.Value * .01f);
            UpmixingSettings.Default.Save();
            Close();
        }

        /// <summary>
        /// Close the window while not saving the settings.
        /// </summary>
        void Cancel(object _, RoutedEventArgs e) => Close();
    }
}