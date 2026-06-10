using System;
using System.Windows.Controls;

using Cavernize.Logic.Models;

namespace CavernizeGUI.UserControls;

/// <summary>
/// Displays the most important metadata of the currently selected <see cref="CavernizeTrack"/>.
/// </summary>
public partial class TrackInfo : UserControl {
    /// <summary>
    /// The track of which the info to display.
    /// </summary>
    public CavernizeTrack SelectedTrack {
        get => selectedTrack;
        set {
            if (value == null) {
                Reset();
                return;
            }

            codec.Text = value.FormatHeader;
            (string property, string value)[] details = value.Details;
            int fill = Math.Min(table.Length, details.Length);
            for (int i = 0; i < fill; i++) {
                table[i].title.Text = details[i].property;
                table[i].value.Text = details[i].value;
            }
            for (int i = fill; i < table.Length; i++) {
                table[i].title.Text = string.Empty;
                table[i].value.Text = string.Empty;
            }
            selectedTrack = value;
        }
    }
    CavernizeTrack selectedTrack;

    /// <summary>
    /// The fields that show key-value pairs in a table.
    /// </summary>
    readonly (TextBlock title, TextBlock value)[] table;

    /// <summary>
    /// Displays the most important metadata of the currently selected <see cref="CavernizeTrack"/>.
    /// </summary>
    public TrackInfo() {
        InitializeComponent();
        table = [
            (row1Title, row1Value),
            (row2Title, row2Value),
            (row3Title, row3Value)
        ];
    }

    /// <summary>
    /// Clear all displayed text.
    /// </summary>
    public void Reset() {
        codec.Text = string.Empty;
        for (int i = 0; i < table.Length; i++) {
            table[i].title.Text = string.Empty;
            table[i].value.Text = string.Empty;
        }
        selectedTrack = null;
    }
}
