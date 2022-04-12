using Cavern.Remapping;
using System;
using System.Windows.Controls;

namespace WAVChannelReorderer {
    internal class ChannelComboBox : ComboBox {
        public ChannelComboBox() => ItemsSource = Enum.GetValues(typeof(ReferenceChannel));
    }
}