using System;
using System.Windows.Controls;

using Cavern.Channels;
using Cavern.WPF.Consts;

namespace FilterStudio.Windows.PipelineSteps {
    /// <summary>
    /// Sets up a single channel's crossover in <see cref="CrossoverSetup"/>.
    /// </summary>
    public partial class CrossoverSetup : UserControl {
        /// <summary>
        /// The channel for which this crossover is set up.
        /// </summary>
        public ReferenceChannel Channel { get; }

        /// <summary>
        /// The frequency where this channel's crossover is set or null if the crossover is disabled.
        /// </summary>
        public int? Frequency => enabled.IsChecked.Value ? freq.Value : null;

        /// <summary>
        /// Sets up a single channel's crossover in <see cref="CrossoverSetup"/>.
        /// </summary>
        public CrossoverSetup(ReferenceChannel channel) {
            Resources.MergedDictionaries.Add(new() {
                Source = new Uri($";component/Resources/Styles.xaml", UriKind.RelativeOrAbsolute)
            });
            Resources.MergedDictionaries.Add(Consts.Language.GetCrossoverDialogStrings());

            InitializeComponent();
            Channel = channel;
            channelName.Text = channel.Translate();
            enabled.IsChecked = channel != ReferenceChannel.ScreenLFE;
        }
    }
}