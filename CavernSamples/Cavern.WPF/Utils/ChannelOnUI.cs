using Cavern.Channels;
using Cavern.WPF.Consts;

namespace Cavern.WPF.Utils {
    /// <summary>
    /// Used to display a channel's name on a UI in the user's language and contain which <see cref="ReferenceChannel"/> it is.
    /// </summary>
    public class ChannelOnUI(ReferenceChannel channel) {
        /// <summary>
        /// The displayed channel.
        /// </summary>
        public ReferenceChannel Channel = channel;

        /// <inheritdoc/>
        public override string ToString() => Channel.Translate();
    }
}