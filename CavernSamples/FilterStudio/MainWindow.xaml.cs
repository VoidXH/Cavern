using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.WpfGraphControl;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Cavern;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;
using Cavern.Utilities;
using VoidX.WPF;

using FilterStudio.Graphs;
using FilterStudio.Resources;

namespace FilterStudio {
    /// <summary>
    /// Main window of Cavern Filter Studio.
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// Source of language strings.
        /// </summary>
        static readonly ResourceDictionary language = Consts.Language.GetMainWindowStrings();

        /// <summary>
        /// The currently selected filter's node.
        /// </summary>
        StyledNode SelectedFilter => (StyledNode)viewer.Graph?.Nodes.FirstOrDefault(x => x.Attr.LineWidth > 1);

        /// <summary>
        /// Handles displaying and manipulating the graph.
        /// </summary>
        readonly GraphViewer viewer;

        /// <summary>
        /// Each channel's full filter graph.
        /// </summary>
        (string name, FilterGraphNode root)[] rootNodes;

        /// <summary>
        /// Any setting has changed in the application and it should be saved.
        /// </summary>
        bool settingChanged;

        /// <summary>
        /// Main window of Cavern Filter Studio.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            viewer = new GraphViewer();
            viewer.BindToPanel(graph);

            showInstructions.IsChecked = Settings.Default.showInstructions;
            SetInstructions(null, null);
            Settings.Default.SettingChanging += (_, e) => settingChanged |= !Settings.Default[e.SettingName].Equals(e.NewValue);
        }

        /// <summary>
        /// Displays an error message.
        /// </summary>
        static void Error(string message) => MessageBox.Show(message, (string)language["Error"], MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>
        /// Save persistent settings on quit.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            Settings.Default.showInstructions = showInstructions.IsChecked;
            if (settingChanged) {
                Settings.Default.Save();
            }
            base.OnClosed(e);
        }

        /// <summary>
        /// When selecting a <paramref name="node"/>, open it for modification.
        /// </summary>
        void OnNodeSelected() {
            StyledNode node = SelectedFilter;
            if (node == null || node.Filter == null) {
                selectedNode.Text = (string)language["NNode"];
                properties.ItemsSource = Array.Empty<object>();
                return;
            }

            selectedNode.Text = node.LabelText;
            properties.ItemsSource = new ObjectToDataGrid(node.Filter.Filter, FilterPropertyChanged, e => Error(e.Message));
        }

        /// <summary>
        /// Updates the graph based on the <see cref="rootNodes"/>.
        /// </summary>
        void ReloadGraph() {
            if (rootNodes != null) {
                viewer.Graph = Parsing.ParseConfigurationFile(rootNodes, Parsing.ParseBackground((SolidColorBrush)Background));
                OnNodeSelected();
            }
        }

        /// <summary>
        /// Force select a node on the graph by <paramref name="uid"/>.
        /// </summary>
        void SelectNode(string uid) {
            viewer.Graph.FindNode(uid).Attr.LineWidth = 2;
            Dispatcher.BeginInvoke(() => { // Call after the graph was redrawn
                OnNodeSelected();
            });
        }

        /// <summary>
        /// Update the name of a filter when any property of it was modified.
        /// </summary>
        void FilterPropertyChanged() {
            StyledNode node = SelectedFilter;
            Filter modified = node?.Filter?.Filter;
            if (modified != null) {
                string newDisplayName = modified.ToString();
                if (node.LabelText != newDisplayName) {
                    node.LabelText = newDisplayName;
                    selectedNode.Text = node.LabelText;
                    ReloadGraph();
                    SelectNode(node.Id);
                }
            }
        }

        /// <summary>
        /// Open a configuration file of known formats.
        /// </summary>
        void LoadConfiguration(object _, RoutedEventArgs e) {
            OpenFileDialog dialog = new() {
                Filter = (string)language["OpFil"]
            };
            if (dialog.ShowDialog().Value) {
                ConfigurationFile file = new EqualizerAPOConfigurationFile(dialog.FileName, Listener.DefaultSampleRate);

                rootNodes = file.InputChannels;
                ReloadGraph();
            }
        }

        /// <summary>
        /// Handle when the instructions are enabled or disabled.
        /// </summary>
        void SetInstructions(object _, RoutedEventArgs e) {
            Visibility instructions = showInstructions.IsChecked ? Visibility.Visible : Visibility.Hidden;
            help1.Visibility = instructions;
            help2.Visibility = instructions;
        }

        /// <summary>
        /// Shows information about the used Cavern library and its version.
        /// </summary>
        void About(object _, RoutedEventArgs e) => MessageBox.Show(Listener.Info, (string)language["HAbou"]);

        /// <summary>
        /// When the user lost the graph because it was moved outside the screen, this function redisplays it in the center of the frame.
        /// </summary>
        void Recenter(object _, RoutedEventArgs e) => ReloadGraph();

        /// <summary>
        /// Delete the currently selected node.
        /// </summary>
        void DeleteNode(object sender, RoutedEventArgs e) {
            StyledNode node = GetSelectedNode(sender);
            if (node == null || node.Filter == null) {
                Error((string)language["NFNod"]);
            } else if (node.Filter.Filter is InputChannel) {
                Error((string)language["NFInp"]);
            } else if (node.Filter.Filter is OutputChannel) {
                Error((string)language["NFOut"]);
            } else {
                node.Filter.DetachFromGraph();
                ReloadGraph();
            }
        }

        /// <summary>
        /// Hack to provide a Click event for MSAGL's WPF library.
        /// </summary>
        void GraphClick(object _, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                StyledNode node = SelectedFilter;
                if (node != null) {
                    node.Attr.LineWidth = 1; // Nodes selected with SelectNode are not actually selected, just were widened
                }
                Dispatcher.BeginInvoke(() => { // Call after the graph has handled it
                    OnNodeSelected();
                });
            } else {
                IViewerObject element = viewer.ObjectUnderMouseCursor;
                List<(string, Action<object, RoutedEventArgs>)> menuItems = [
                    ((string)language["FLabe"], (_, e) => AddLabel(element, e)),
                    ((string)language["FGain"], (_, e) => AddGain(element, e)),
                    ((string)language["FDela"], (_, e) => AddDelay(element, e)),
                    ((string)language["FBiqu"], (_, e) => AddBiquad(element, e)),
                ];
                if (element is IViewerNode) {
                    menuItems.Add((null, null));
                    menuItems.Add(((string)language["CoDel"], (_, e) => DeleteNode(element, e)));
                }
                QuickContextMenu.Show(menuItems);
            }
        }
    }
}