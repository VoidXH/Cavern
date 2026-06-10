using System.Windows.Controls;
using System.Windows.Media;

namespace CavernizeGUI.UserControls {
    /// <summary>
    /// An icon and a text in a single control.
    /// </summary>
    public partial class TextWithIcon : UserControl {
        /// <summary>
        /// The displayed text of the item that needs both a text and an icon.
        /// </summary>
        public string Text {
            get => text.Text;
            set => text.Text = value;
        }

        /// <summary>
        /// Icon displayed on the left of the <see cref="text"/>.
        /// </summary>
        public ImageSource Icon {
            get => icon.Source;
            set => icon.Source = value;
        }

        /// <summary>
        /// An icon and a text in a single control.
        /// </summary>
        public TextWithIcon() => InitializeComponent();
    }
}
