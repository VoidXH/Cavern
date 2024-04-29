using System.Windows;
using System.Windows.Input;

using Cavern;
using Cavern.Filters;
using Cavern.WPF;

using FilterStudio.Graphs;

namespace FilterStudio {
    // Click handlers of each item under "Add filter"
    partial class MainWindow {
        /// <summary>
        /// Add a new <see cref="Gain"/> filter to the graph.
        /// </summary>
        void AddGain(object _, RoutedEventArgs e) => AddFilter(new Gain(0));

        /// <summary>
        /// Add a new <see cref="Delay"/> filter to the graph.
        /// </summary>
        void AddDelay(object _, RoutedEventArgs e) => AddFilter(new Delay(0, Listener.DefaultSampleRate));

        /// <summary>
        /// Add a new <see cref="BiquadFilter"/> to the graph.
        /// </summary>
        void AddBiquad(object _, RoutedEventArgs e) {
            string error = PreFilterAddingChecks();
            if (error == null) {
                BiquadEditor editor = new BiquadEditor {
                    Background = Background,
                    Resources = Resources
                };
                if (editor.ShowDialog().Value) {
                    FinalizeFilter(editor.Filter);
                }
            } else {
                Error(error);
            }
        }

        /// <summary>
        /// Checks to perform before a filter can be added. If an error happens, returns its message, otherwise null.
        /// </summary>
        string PreFilterAddingChecks() {
            StyledNode node = SelectedFilter;
            if (node == null) {
                return (string)language["NNode"];
            } else if (node.Filter.Filter is OutputChannel) {
                return (string)language["NFLas"];
            }
            return null;
        }

        /// <summary>
        /// Add a basic filter after the currently selected graph node.
        /// </summary>
        void AddFilter(Filter filter) {
            string error = PreFilterAddingChecks();
            if (error == null) {
                FinalizeFilter(filter);
            } else {
                Error(error);
            }
        }

        /// <summary>
        /// When the <see cref="PreFilterAddingChecks"/> have passed, and the filter has been determined, add it to the graph.
        /// </summary>
        void FinalizeFilter(Filter filter) {
            if (filterShift.IsChecked || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                SelectedFilter.Filter.AddChild(filter);
            } else {
                SelectedFilter.Filter.AddBeforeChildren(filter);
            }
            ReloadGraph();
        }
    }
}