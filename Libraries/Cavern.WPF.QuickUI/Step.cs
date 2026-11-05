using System.Windows.Controls;
using System.Windows.Media;

namespace Cavern.WPF.QuickUI {
    /// <summary>
    /// A single step in a <see cref="QuickShell"/>.
    /// </summary>
    public class Step {
        /// <summary>
        /// The button that activates the step.
        /// </summary>
        internal Button button;

        /// <summary>
        /// The page shown when the step is active.
        /// </summary>
        internal UserControl page;

        /// <summary>
        /// Whether the step can be activated.
        /// </summary>
        bool enabled = true;

        /// <summary>
        /// Whether the step is currently active.
        /// </summary>
        bool active;

        /// <summary>
        /// Creates a step.
        /// </summary>
        internal Step(UserControl page) => this.page = page;

        /// <summary>
        /// Gets the button that activates the step.
        /// </summary>
        public Button Button {
            get => button;
            internal set => button = value;
        }

        /// <summary>
        /// Gets the page shown when the step is active.
        /// </summary>
        public UserControl Page => page;

        /// <summary>
        /// Gets the icon displayed in the step button.
        /// </summary>
        public ImageSource Icon { get; internal set; }

        /// <summary>
        /// Gets or sets whether the step can be activated.
        /// </summary>
        public bool IsEnabled {
            get => enabled;
            set {
                enabled = value;
                button.IsEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the step is currently active.
        /// </summary>
        public bool IsActive {
            get => active;
            internal set => active = value;
        }
    }
}
