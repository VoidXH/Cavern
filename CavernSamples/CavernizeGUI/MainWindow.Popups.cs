using System.Text;
using System.Windows;

using Cavern.Remapping;

using CavernizeGUI.Elements;
using CavernizeGUI.Windows;

namespace CavernizeGUI {
    public partial class MainWindow {
        /// <summary>
        /// Opens the upmixing settings.
        /// </summary>
        void OpenUpmixSetup(object _, RoutedEventArgs e) {
            UpmixingSetup setup = new UpmixingSetup {
                Title = (string)language["UpmiW"]
            };
            setup.ShowDialog();
        }

        /// <summary>
        /// Show the post-render report in a popup.
        /// </summary>
        void ShowPostRenderReport(object _, RoutedEventArgs e) => MessageBox.Show(report, (string)language["PReRe"]);

        /// <summary>
        /// Shows a popup about what channel should be wired to which output.
        /// </summary>
        void DisplayWiring(object _, RoutedEventArgs e) {
            ReferenceChannel[] channels = ((RenderTarget)renderTarget.SelectedItem).Channels;
            ChannelPrototype[] prototypes = ChannelPrototype.Get(channels);
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < prototypes.Length; ++i) {
                output.AppendLine(string.Format((string)language["ChCon"], prototypes[i].Name,
                    ChannelPrototype.Get(i, prototypes.Length).Name));
            }
            MessageBox.Show(output.ToString(), (string)language["WrGui"]);
        }
    }
}