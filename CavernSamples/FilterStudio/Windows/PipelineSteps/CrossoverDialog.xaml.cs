using System;
using System.Linq;

using Cavern.Channels;
using Cavern.QuickEQ.Crossover;
using Cavern.WPF.BaseClasses;
using Cavern.WPF.Utils;

namespace FilterStudio.Windows.PipelineSteps {
    /// <summary>
    /// Adds a new pipeline step with a crossover pre-created.
    /// </summary>
    public partial class CrossoverDialog : OkCancelDialog {
        /// <summary>
        /// The frequencies where each channel's crossover is set or null if a channel's crossover is disabled.
        /// </summary>
        public (ReferenceChannel channel, int? frequency)[] Crossovers {
            get {
                (ReferenceChannel, int?)[] result = new (ReferenceChannel, int?)[channels.Items.Count];
                for (int i = 0; i < result.Length; i++) {
                    CrossoverSetup setup = (CrossoverSetup)channels.Items[i];
                    result[i] = (setup.Channel, setup.Frequency);
                }
                return result;
            }
        }

        /// <summary>
        /// The selected crossover creation algorithm.
        /// </summary>
        public CrossoverType Type => ((CrossoverTypeOnUI)crossoverType.SelectedItem).Type;

        /// <summary>
        /// The channel to put low frequencies to.
        /// </summary>
        public ReferenceChannel Target => ((ChannelOnUI)targetChannel.SelectedItem).Channel;

        /// <summary>
        /// Adds a new pipeline step with a crossover pre-created.
        /// </summary>
        public CrossoverDialog(params ReferenceChannel[] filterSetChannels) {
            Resources.MergedDictionaries.Add(new() {
                Source = new Uri($";component/Resources/Styles.xaml", UriKind.RelativeOrAbsolute)
            });
            Resources.MergedDictionaries.Add(Cavern.WPF.Consts.Language.GetCommonStrings());
            Resources.MergedDictionaries.Add(Consts.Language.GetCrossoverDialogStrings());

            InitializeComponent();
            for (int i = 0; i < filterSetChannels.Length; i++) {
                channels.Items.Add(new CrossoverSetup(filterSetChannels[i]));
            }
            crossoverType.ItemsSource = ((CrossoverType[])Enum.GetValues(typeof(CrossoverType))).Select(x => new CrossoverTypeOnUI(x));
            crossoverType.SelectedIndex = 0;
            targetChannel.ItemsSource = filterSetChannels.Select(x => new ChannelOnUI(x));
            int lfeIndex = Array.IndexOf(filterSetChannels, ReferenceChannel.ScreenLFE);
            targetChannel.SelectedIndex = lfeIndex != -1 ? lfeIndex : 0;
        }
    }
}