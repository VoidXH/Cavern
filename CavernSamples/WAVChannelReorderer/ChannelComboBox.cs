using System;
using System.Windows.Controls;

using Cavern.Channels;

namespace WAVChannelReorderer {
    internal class ChannelComboBox : ComboBox {
        public ChannelComboBox() => ItemsSource = Enum.GetValues(typeof(ReferenceChannel));
    }
}