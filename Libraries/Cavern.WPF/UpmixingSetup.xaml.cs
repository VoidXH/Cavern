using System.Windows;
using System.Windows.Media;

using Cavern.CavernSettings;
using Cavern.WPF.BaseClasses;

namespace Cavern.WPF {
    /// <summary>
    /// Configures a repository of <see cref="UpmixingSettings"/>.
    /// </summary>
    public partial class UpmixingSetup : OkCancelDialog {
        /// <summary>
        /// Settings to change if the user clicks "OK" in this window.
        /// </summary>
        readonly UpmixingSettings settings;

        /// <summary>
        /// Configures a repository of upmixing <paramref name="settings"/>.
        /// </summary>
        public UpmixingSetup(UpmixingSettings settings) : this(settings, null, null) { }

        /// <summary>
        /// Configures a repository of upmixing <paramref name="settings"/> while matching the style of another <see cref="Window"/>.
        /// </summary>
        public UpmixingSetup(UpmixingSettings settings, Brush background, ResourceDictionary resources) {
            if (resources != null) {
                Resources.MergedDictionaries.Add(resources);
            }
            Resources.MergedDictionaries.Add(Consts.Language.GetCommonStrings());
            Resources.MergedDictionaries.Add(Consts.Language.GetUpmixingSetupStrings());
            InitializeComponent();
            if (background != null) {
                Background = background;
            }

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

        /// <inheritdoc/>
        protected override void OK(object _, RoutedEventArgs e) {
            settings.MatrixUpmixing = matrixUpmix.IsChecked.Value;
            settings.Cavernize = cavernize.IsChecked.Value;
            settings.Effect = (float)(effect.Value * .01f);
            settings.Smoothness = (float)(smoothness.Value * .01f);
            base.OK(_, e);
        }
    }
}
