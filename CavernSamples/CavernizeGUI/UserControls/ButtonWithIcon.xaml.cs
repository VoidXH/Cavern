using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CavernizeGUI.UserControls {
    /// <summary>
    /// Cavernize-design compatible button with an icon on the left of the text.
    /// </summary>
    public partial class ButtonWithIcon : UserControl {
        /// <summary>
        /// The button can either be primary (large, colorful) or secondary (small, blending in the background).
        /// </summary>
        public bool Primary {
            get => root.Style == Resources["PrimaryButtonStyle"];
            set {
                root.Style = value ? (Style)Resources["PrimaryButtonStyle"] : (Style)Resources["SecondaryButtonStyle"];
                text.Style = value ? (Style)Resources["PrimaryText"] : (Style)Resources["SecondaryText"];
            }
        }

        /// <summary>
        /// Text displayed on the button.
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
        /// Operation to be performed when the button is clicked.
        /// </summary>
        public event RoutedEventHandler Click;

        /// <summary>
        /// Cavernize-design compatible button with an icon on the left of the text.
        /// </summary>
        public ButtonWithIcon() {
            InitializeComponent();
            Primary = false;
        }

        /// <summary>
        /// Perform the assigned event when the button is clicked.
        /// </summary>
        void OnClick(object sender, RoutedEventArgs e) => Click?.Invoke(this, e);
    }
}