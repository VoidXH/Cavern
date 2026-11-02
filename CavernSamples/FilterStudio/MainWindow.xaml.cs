using Microsoft.Msagl.Drawing;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cavern;
using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Interfaces;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;
using Cavern.Format.Utilities;
using Cavern.Utilities;
using Cavern.WPF;

using FilterStudio.Graphs;
using FilterStudio.Resources;

namespace FilterStudio {
    /// <summary>
    /// Main window of Cavern Filter Studio.
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// The currently selected project sample rate.
        /// </summary>
        int SampleRate => (int)sampleRate.SelectedItem;

        /// <summary>
        /// Source of language strings.
        /// </summary>
        static readonly ResourceDictionary language = Consts.Language.GetMainWindowStrings();

        /// <summary>
        /// The user-selected channels used when new configurations are created.
        /// </summary>
        ReferenceChannel[] channels;

        /// <summary>
        /// Each channel's full filter graph.
        /// </summary>
        IFilterGraphNode[] rootNodes;

        /// <summary>
        /// Any setting has changed in the application and it should be saved.
        /// </summary>
        bool settingChanged;

        /// <summary>
        /// Main window of Cavern Filter Studio.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            pipeline.OnRightClick += PipelineRightClick;
            pipeline.OnSplitChanged += SplitChanged;
            pipeline.background = Parsing.ParseBackground((SolidColorBrush)Background);
            pipeline.language = language;
            graph.OnLeftClick += GraphLeftClick;
            graph.OnRightClick += GraphRightClick;
            graph.OnConnect += GraphConnect;

            showInstructions.IsChecked = Settings.Default.showInstructions;
            SetInstructions(null, null);
            SetDirection((LayerDirection)Settings.Default.graphDirection);
            channels = ReferenceChannelExtensions.FromMask(Settings.Default.channels);
            sampleRate.ItemsSource = sampleRates;
            sampleRate.SelectedItem = Settings.Default.sampleRate;
            Settings.Default.SettingChanging += (_, e) => settingChanged |= !Settings.Default[e.SettingName].Equals(e.NewValue);
        }

        /// <summary>
        /// If a filter can be translated to the user's language, return that instead of a regular ToString.
        /// </summary>
        static string FilterToString(Filter filter) => filter is ILocalizableToString loc ? loc.ToString(CultureInfo.CurrentCulture) : filter.ToString();

        /// <summary>
        /// Displays a message for a successful operation.
        /// </summary>
        static void Success(string message) => MessageBox.Show(message, (string)language["Succe"], MessageBoxButton.OK, MessageBoxImage.Information);

        /// <summary>
        /// Displays a warning message to the user.
        /// </summary>
        static void Warning(string message) => MessageBox.Show(message, (string)language["Warni"], MessageBoxButton.OK, MessageBoxImage.Warning);

        /// <summary>
        /// Displays an error message.
        /// </summary>
        static void Error(string message) => MessageBox.Show(message, (string)language["Error"], MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>
        /// Warns the user about something potentially destructive happening, and asks them if they want to continue.
        /// </summary>
        static bool WarningYesNo(string question) =>
            MessageBox.Show(question, (string)language["Warni"], MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

        /// <summary>
        /// Save persistent settings on quit.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            Settings.Default.showInstructions = showInstructions.IsChecked;
            Settings.Default.graphDirection = (byte)graphDirection;
            Settings.Default.sampleRate = (int)sampleRate.SelectedItem;
            if (settingChanged) {
                Settings.Default.Save();
            }
            base.OnClosed(e);
        }

        /// <summary>
        /// Handle when the instructions are enabled or disabled.
        /// </summary>
        void SetInstructions(object _, RoutedEventArgs e) {
            Visibility instructions = showInstructions.IsChecked ? Visibility.Visible : Visibility.Hidden;
            help1.Visibility = instructions;
            help2.Visibility = instructions;
            help3.Visibility = instructions;
        }

        /// <summary>
        /// Open a configuration file dropped onto the window.
        /// </summary>
        void OnFileDropped(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files) {
                    if (Directory.Exists(file)) {
                        foreach (string nestedFile in Directory.GetFiles(file, "*.*", SearchOption.AllDirectories)) {
                            OpenConfigurationFile(nestedFile);
                        }
                    } else {
                        OpenConfigurationFile(file);
                    }
                }
            }
        }

        /// <summary>
        /// A different split of the edited file is selected.
        /// </summary>
        void SplitChanged(IFilterGraphNode[] splitRoots) {
            rootNodes = splitRoots;
            ReloadGraph();
        }

        /// <summary>
        /// Update the UI to the changes that happened in the filter.
        /// </summary>
        void FilterPropertyChanged() {
            StyledNode node = graph.SelectedNode;
            Filter modified = node?.Filter?.Filter;
            if (modified != null) {
                string newDisplayName = modified.ToString();
                if (node.LabelText != newDisplayName) { // If the displayed name changed, reload the graph
                    node.LabelText = newDisplayName;
                    selectedNode.Text = node.LabelText;
                    ReloadGraph();
                }
                // Otherwise only the property panel
                graph.SelectNode(node.Id);
            }
        }

        /// <summary>
        /// Get the channels that are used in the current configuration or will be used when a new configuration will be created.
        /// </summary>
        ReferenceChannel[] GetChannels() => rootNodes == null ? channels : [.. rootNodes.Select(x => ((InputChannel)x.Filter).Channel)];

        /// <summary>
        /// All possible project sample rates.
        /// </summary>
        static readonly int[] sampleRates = [8000, 11025, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000, 384000];
    }
}
