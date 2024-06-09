using System.Windows;
using System.Windows.Controls;

using Cavern.Channels;
using Cavern.WPF.BaseClasses;

namespace Cavern.WPF {
    /// <summary>
    /// Channel layout selector dialog.
    /// </summary>
    public partial class ChannelSelector : OkCancelDialog {
        /// <summary>
        /// Set when OK is clicked, contains the channels selected by the user.
        /// </summary>
        public ReferenceChannel[] SelectedChannels {
            get => channelMap.Where(x => x.Value.IsChecked.Value).Select(x => x.Key).ToArray();
            set {
                foreach (KeyValuePair<ReferenceChannel, CheckBox> channel in channelMap) {
                    channel.Value.IsChecked = false;
                }
                for (int i = 0; i < value.Length; i++) {
                    channelMap[value[i]].IsChecked = true;
                }
            }
        }

        /// <summary>
        /// Which <see cref="CheckBox"/> is responsible for setting each <see cref="ReferenceChannel"/>.
        /// </summary>
        readonly Dictionary<ReferenceChannel, CheckBox> channelMap;

        /// <summary>
        /// Channel layout selector dialog.
        /// </summary>
        public ChannelSelector() {
            Resources.MergedDictionaries.Add(new() {
                Source = new Uri($"/Cavern.WPF;component/Resources/ChannelSelectorStyle.xaml", UriKind.RelativeOrAbsolute)
            });
            Resources.MergedDictionaries.Add(Consts.Language.GetCommonStrings());
            Resources.MergedDictionaries.Add(Consts.Language.GetChannelSelectorStrings());
            InitializeComponent();
            channelMap = new Dictionary<ReferenceChannel, CheckBox> {
                [ReferenceChannel.FrontLeft] = frontLeft,
                [ReferenceChannel.FrontLeftCenter] = frontLeftCenter,
                [ReferenceChannel.FrontCenter] = frontCenter,
                [ReferenceChannel.FrontRightCenter] = frontRightCenter,
                [ReferenceChannel.FrontRight] = frontRight,
                [ReferenceChannel.WideLeft] = wideLeft,
                [ReferenceChannel.WideRight] = wideRight,
                [ReferenceChannel.SideLeft] = sideLeft,
                [ReferenceChannel.ScreenLFE] = screenLFE,
                [ReferenceChannel.SideRight] = sideRight,
                [ReferenceChannel.RearLeft] = rearLeft,
                [ReferenceChannel.RearCenter] = rearCenter,
                [ReferenceChannel.RearRight] = rearRight,
                [ReferenceChannel.TopFrontLeft] = topFrontLeft,
                [ReferenceChannel.TopFrontCenter] = topFrontCenter,
                [ReferenceChannel.TopFrontRight] = topFrontRight,
                [ReferenceChannel.TopSideLeft] = topSideLeft,
                [ReferenceChannel.GodsVoice] = godsVoice,
                [ReferenceChannel.TopSideRight] = topSideRight,
                [ReferenceChannel.TopRearLeft] = topRearLeft,
                [ReferenceChannel.TopRearCenter] = topRearCenter,
                [ReferenceChannel.TopRearRight] = topRearRight,
            };
        }
    }
}