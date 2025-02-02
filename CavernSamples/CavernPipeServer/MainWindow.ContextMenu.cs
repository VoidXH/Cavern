using System;
using System.Windows;
using System.Windows.Forms;

using Cavern;
using Cavern.Channels;
using Cavern.Internals;
using Cavern.WPF.Consts;

using Application = System.Windows.Application;
using GLanguage = Cavern.WPF.Consts.Language;
using MessageBox = System.Windows.MessageBox;

namespace CavernPipeServer {
    // Functions related to the tray icon right click menu
    partial class MainWindow {
        /// <summary>
        /// Overwrite the layout Cavern is using, globally.
        /// </summary>
        /// <remarks>Read <see cref="CavernConfiguration.IKnowWhatIAmDoing"/> when using this code anywhere!</remarks>
        void SetLayout(string name, ReferenceChannel[] channels) {
            if (GLanguage.Warning(string.Format((string)language["WSLay"], name), MessageBoxButton.YesNo) == MessageBoxResult.No) {
                return;
            }

            Listener.ReplaceChannels(ChannelPrototype.ToLayoutAlternative(channels));
            CavernConfiguration.IKnowWhatIAmDoing = true;
            CavernConfiguration.SaveCurrentLayoutAsDefault();
            GLanguage.Warning((string)language["WSLaC"]);
        }

        /// <summary>
        /// Create a right click menu to be assigned to the tray icon.
        /// </summary>
        ContextMenuStrip CreateContextMenu() {
            ContextMenuStrip result = new();
            result.Items.Add((string)language["MOpen"], null, Open);
            result.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem settingsMenu = new((string)language["MSett"]);
            ToolStripMenuItem layouts = new((string)language["MSLay"]);
            for (int i = 0; i < customLayouts.Length; i++) {
                (string name, ReferenceChannel[] channels) = customLayouts[i];
                layouts.DropDownItems.Add(customLayouts[i].name, null, (_, __) => SetLayout(name, channels));
            }
            settingsMenu.DropDownItems.Add(layouts);
            settingsMenu.DropDownItems.Add((string)language["MWiri"], null, ShowWiring);
            result.Items.Add(settingsMenu);
            result.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem diagMenu = new((string)language["MDiag"]);
            diagMenu.DropDownItems.Add((string)language["MRest"], null, Restart);
            diagMenu.DropDownItems.Add((string)language["MLErr"], null, LastError);
            result.Items.Add(diagMenu);
            result.Items.Add(new ToolStripSeparator());

            result.Items.Add((string)language["MExit"], null, Exit);
            return result;
        }

        /// <summary>
        /// Show the configuration dialog.
        /// </summary>
        void Open(object _, EventArgs e) {
            Show();
            WindowState = WindowState.Normal;
        }

        /// <summary>
        /// Show how the user should wire spatial speakers to standard outputs.
        /// </summary>
        void ShowWiring(object _, EventArgs e) {
            new Listener(); // Load current user layout
            ChannelPrototype.GetReferences(Listener.Channels).DisplayWiring();
        }

        /// <summary>
        /// Try to restart the pipe.
        /// </summary>
        void Restart(object _, EventArgs __) {
            try {
                handler.Start();
            } catch (InvalidOperationException e) {
                GLanguage.Error(e.Message);
            }
        }

        /// <summary>
        /// Display the cause of the last disconnection.
        /// </summary>
        void LastError(object _, EventArgs __) =>
            MessageBox.Show(lastError ?? (string)language["NLErr"], (string)language["MLErr"], MessageBoxButton.OK, MessageBoxImage.Information);

        /// <summary>
        /// Close the CavernPipe server.
        /// </summary>
        void Exit(object _, EventArgs e) {
            exiting = true;
            handler.Dispose();
            icon.Dispose();
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Channel layouts that can be used for overriding the Cavern Driver setting.
        /// </summary>
        static readonly (string name, ReferenceChannel[] channels)[] customLayouts = [
            ("3.1.2", ChannelPrototype.ref312),
            ("4.0.4", ChannelPrototype.ref404),
            ("4.1.1", ChannelPrototype.ref411),
            ("4.1.3", ChannelPrototype.ref413),
            ("5.1", ChannelPrototype.ref510),
            ("5.1.2", ChannelPrototype.ref512),
            ("7.1", ChannelPrototype.ref710),
        ];
    }
}