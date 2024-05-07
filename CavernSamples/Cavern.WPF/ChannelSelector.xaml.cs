using System.Windows;
using System.Windows.Controls;

using Cavern.Channels;

namespace Cavern.WPF {
    /// <summary>
    /// Channel layout selector dialog.
    /// </summary>
    public partial class ChannelSelector : Window {
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

        /// <summary>
        /// Closes the dialog with the filter selected.
        /// </summary>
        void OK(object _, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Closes the dialog with no filter selected.
        /// </summary>
        void Cancel(object _, RoutedEventArgs e) => Close();
    }
}