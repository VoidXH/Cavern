using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace VoidX.WPF {
    /// <summary>
    /// A way to create a <see cref="ContextMenu"/> with an array of pre-selected options with less code.
    /// </summary>
    public class QuickContextMenu : ContextMenu {
        /// <summary>
        /// A way to create a <see cref="ContextMenu"/> with an array of pre-selected <paramref name="options"/> with less code.
        /// </summary>
        /// <param name="options">Items to put on the list of selectable options, null headers become separators</param>
        public QuickContextMenu(IEnumerable<(string header, Action<object, RoutedEventArgs> handler)> options) {
            foreach ((string header, Action<object, RoutedEventArgs> handler) in options) {
                if (header == null) {
                    Items.Add(new Separator());
                    continue;
                }

                MenuItem item = new MenuItem {
                    Header = header
                };
                item.Click += new RoutedEventHandler(handler);
                Items.Add(item);
            }
        }

        /// <summary>
        /// Display a single use <see cref="QuickContextMenu"/> of the selected <paramref name="options"/>.
        /// </summary>
        /// <param name="options">Items to put on the list of selectable options, null headers become separators</param>
        public static void Show(IEnumerable<(string header, Action<object, RoutedEventArgs> handler)> options) =>
            new QuickContextMenu(options).IsOpen = true;
    }
}