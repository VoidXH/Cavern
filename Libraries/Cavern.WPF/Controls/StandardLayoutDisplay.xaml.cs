using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using Cavern.Channels;
using Cavern.Format.Common;

namespace Cavern.WPF.Controls {
    /// <summary>
    /// Displays standard spatial speaker layouts with the enabled channels.
    /// </summary>
    public partial class StandardLayoutDisplay : UserControl {
        /// <summary>
        /// The matching displayed dot's color for a supported channel.
        /// </summary>
        public Brush this[ReferenceChannel channel] {
            get {
                if (channels.TryGetValue(channel, out Ellipse display)) {
                    return display.Fill;
                } else {
                    throw new InvalidChannelException(ChannelPrototype.GetName(channel));
                }
            }

            set {
                if (channels.TryGetValue(channel, out Ellipse display)) {
                    display.Fill = value;
                } else {
                    throw new InvalidChannelException(ChannelPrototype.GetName(channel));
                }
            }
        }

        /// <summary>
        /// Set the color of all channels' dots.
        /// </summary>
        public Brush All {
            set {
                foreach (KeyValuePair<ReferenceChannel, Ellipse> pair in channels) {
                    pair.Value.Fill = value;
                }
            }
        }

        /// <summary>
        /// The matching displayed dot for each supported channel.
        /// </summary>
        readonly Dictionary<ReferenceChannel, Ellipse> channels;

        /// <summary>
        /// Displays standard spatial speaker layouts with the enabled channels.
        /// </summary>
        public StandardLayoutDisplay() {
            InitializeComponent();
            channels = new() {
                [ReferenceChannel.FrontLeft] = frontLeft,
                [ReferenceChannel.FrontCenter] = frontCenter,
                [ReferenceChannel.FrontRight] = frontRight,
                [ReferenceChannel.WideLeft] = wideLeft,
                [ReferenceChannel.WideRight] = wideRight,
                [ReferenceChannel.SideLeft] = sideLeft,
                [ReferenceChannel.SideRight] = sideRight,
                [ReferenceChannel.RearLeft] = rearLeft,
                [ReferenceChannel.RearCenter] = rearCenter,
                [ReferenceChannel.RearRight] = rearRight,
                [ReferenceChannel.TopFrontLeft] = topFrontLeft,
                [ReferenceChannel.TopFrontCenter] = topFrontCenter,
                [ReferenceChannel.TopFrontRight] = topFrontRight,
                [ReferenceChannel.TopSideLeft] = topSideLeft,
                [ReferenceChannel.TopSideRight] = topSideRight,
                [ReferenceChannel.TopRearLeft] = topRearLeft,
                [ReferenceChannel.TopRearCenter] = topRearCenter,
                [ReferenceChannel.TopRearRight] = topRearRight
            };
        }
    }
}