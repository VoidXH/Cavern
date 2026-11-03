using System;
using System.Windows;

using Cavern.WPF.BaseClasses;

using FilterStudio.Resources;

namespace FilterStudio.Windows {
    /// <summary>
    /// Shows the application's settings. Currently only covers the phase type used when
    /// synthesizing Graphic EQ filters into convolution filters.
    /// </summary>
    public partial class SettingsDialog : OkCancelDialog {
        /// <summary>
        /// Shows the application's settings.
        /// </summary>
        public SettingsDialog() {
            Resources.MergedDictionaries.Add(new() {
                Source = new Uri($";component/Resources/Styles.xaml", UriKind.RelativeOrAbsolute)
            });
            Resources.MergedDictionaries.Add(Consts.Language.GetSettingsDialogStrings());
            Resources.MergedDictionaries.Add(Cavern.WPF.Consts.Language.GetCommonStrings());
            InitializeComponent();
            minPhase.IsChecked = !Settings.Default.graphicEQLinearPhase;
            linearPhase.IsChecked = Settings.Default.graphicEQLinearPhase;
        }

        /// <summary>
        /// Confirm the dialog and store the chosen settings.
        /// </summary>
        protected override void OK(object _, RoutedEventArgs e) {
            Settings.Default.graphicEQLinearPhase = linearPhase.IsChecked ?? false;
            DialogResult = true;
            Close();
        }
    }
}
