using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cavern.WPF.QuickUI {
    /// <summary>
    /// Page-based shell with a left side icon panel. Each step owns a page (<see cref="UserControl"/>) and an optional icon; activating a step
    /// shows its page and highlights its icon button.
    /// </summary>
    public partial class QuickShell : UserControl {
        /// <summary>
        /// The steps in order.
        /// </summary>
        readonly List<Step> steps = [];

        /// <summary>
        /// The active step index, or -1 if none.
        /// </summary>
        int activeStep = -1;

        /// <summary>
        /// Background used for the active step button.
        /// </summary>
        static readonly Brush activeBackground = new SolidColorBrush(Color.FromRgb(0x0E, 0x63, 0x9C));

        /// <summary>
        /// Border brush used for the active step button.
        /// </summary>
        static readonly Brush activeBorder = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC));

        /// <summary>
        /// Creates the shell.
        /// </summary>
        public QuickShell() => InitializeComponent();

        /// <summary>
        /// Raised when the active step changes. The argument is the new step index.
        /// </summary>
        public event Action<int> StepActivated;

        /// <summary>
        /// Gets the steps in order.
        /// </summary>
        public IReadOnlyList<Step> Steps => steps;

        /// <summary>
        /// Adds a step with a page and no icon.
        /// </summary>
        public Step AddStep(UserControl page) => AddStep(page, null);

        /// <summary>
        /// Adds a step with a page and an icon.
        /// </summary>
        public Step AddStep(UserControl page, ImageSource icon) {
            Step step = new(page) {
                Icon = icon
            };
            Button button = CreateStepButton(step);
            step.Button = button;
            StepPanel.Children.Add(button);
            PagePanel.Children.Add(page);
            page.Visibility = Visibility.Collapsed;
            steps.Add(step);
            return step;
        }

        /// <summary>
        /// Activates the given step and shows its page.
        /// </summary>
        public void SetActiveStep(int step) {
            if (step < 0 || step >= steps.Count || step == activeStep) {
                return;
            }

            activeStep = step;
            for (int i = 0; i < steps.Count; i++) {
                bool isActive = i == step;
                steps[i].IsActive = isActive;
                steps[i].Button.Background = isActive ? activeBackground : Brushes.Transparent;
                steps[i].Button.BorderBrush = isActive ? activeBorder : Brushes.Transparent;
                steps[i].Page.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
            }

            StepActivated?.Invoke(step);
        }

        /// <summary>
        /// Enables or disables a step.
        /// </summary>
        public void SetStepEnabled(int step, bool enabled) => steps[step].IsEnabled = enabled;

        /// <summary>
        /// Creates the button for a step.
        /// </summary>
        Button CreateStepButton(Step step) {
            Button button = new Button {
                Style = TryFindResource("StepButton") as Style,
                IsEnabled = step.IsEnabled,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Content = step.Icon is null ? null : new Image {
                    Width = 32,
                    Height = 32,
                    Source = step.Icon
                }
            };
            button.Click += (s, e) => SetActiveStep(steps.IndexOf(step));
            return button;
        }
    }
}
