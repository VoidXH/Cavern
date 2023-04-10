using System.Windows;
using System.Windows.Controls;

using CavernizeGUI.Elements;

namespace CavernizeGUI.Windows {
    /// <summary>
    /// Interaction logic for the custom render target dropdown.
    /// </summary>
    public partial class RenderTargetSelector : Window {
        /// <summary>
        /// The user-selected <see cref="RenderTarget"/>.
        /// </summary>
        public RenderTarget Result { get; private set; }

        /// <summary>
        /// Initialize the window, add the layouts.
        /// </summary>
        public RenderTargetSelector(RenderTarget[] options, RenderTarget selected) {
            InitializeComponent();
            for (int i = 0; i < options.Length; i++) {
                AttachTarget(options[i], options[i].OutputChannels <= 8 ? pcReadyTemplate : multichannelTemplate, options[i] == selected);
            }

            content.Children.Remove(pcReadyTemplate);
            content.Children.Remove(multichannelTemplate);
        }

        /// <summary>
        /// Attach a render <paramref name="target"/> to a group of targets by modifying a <paramref name="template"/>
        /// </summary>
        void AttachTarget(RenderTarget target, RadioButton template, bool selected) {
            RadioButton newButton = new() {
                Margin = template.Margin,
                VerticalAlignment = VerticalAlignment.Top,
                Content = target.Name,
                IsChecked = selected
            };
            Grid.SetColumn(newButton, Grid.GetColumn(template));
            Grid.SetRow(newButton, Grid.GetRow(template));
            newButton.Click += (_, __) => {
                Result = target;
                Close();
            };
            content.Children.Add(newButton);
            template.Margin = new Thickness(10, template.Margin.Top + 20, 10, 0);
        }
    }
}