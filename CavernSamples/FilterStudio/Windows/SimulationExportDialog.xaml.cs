using System;

using Cavern.WPF.BaseClasses;

namespace FilterStudio.Windows {
    /// <summary>
    /// Settings for the WAV simulation export: LFE headroom and whether to mix down to a single channel.
    /// </summary>
    public partial class SimulationExportDialog : OkCancelDialog {
        /// <summary>
        /// How many dB to take off the non-LFE channels (so LFE stays louder by this amount).
        /// </summary>
        public int LfeHeadroomDb => lfeHeadroom.Value;

        /// <summary>
        /// Whether to mix all channels into a single mono output channel.
        /// </summary>
        public bool MixToSingleChannel => mixToSingle.IsChecked.Value;

        /// <summary>
        /// Settings for the WAV simulation export: LFE headroom and whether to mix down to a single channel.
        /// </summary>
        public SimulationExportDialog() {
            Resources.MergedDictionaries.Add(new() {
                Source = new Uri($";component/Resources/Styles.xaml", UriKind.RelativeOrAbsolute)
            });
            Resources.MergedDictionaries.Add(Consts.Language.GetSimulationExportDialogStrings());
            Resources.MergedDictionaries.Add(Cavern.WPF.Consts.Language.GetCommonStrings());
            InitializeComponent();
        }
    }
}
