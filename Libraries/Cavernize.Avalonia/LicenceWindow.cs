using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

using Cavern.Utilities;

namespace Cavernize.Avalonia;

/// <summary>
/// Window displaying a licence that needs to be accepted before continuing.
/// </summary>
public sealed class LicenceWindow(Window owner, string acceptText, string cancelText) : ILicence {
    string description;
    string licence;

    /// <inheritdoc/>
    public void SetDescription(string description) => this.description = description;

    /// <inheritdoc/>
    public void SetLicenceText(string licence) => this.licence = licence;

    /// <inheritdoc/>
    public bool Prompt() {
        if (Dispatcher.UIThread.CheckAccess()) {
            throw new InvalidOperationException("Licence prompts have to be requested from a background thread.");
        }

        bool accepted = false;
        Exception exception = null;
        using ManualResetEventSlim completed = new();
        Dispatcher.UIThread.Post(async () => {
            try {
                accepted = await PromptOnUI();
            } catch (Exception e) {
                exception = e;
            } finally {
                completed.Set();
            }
        });
        completed.Wait();
        if (exception != null) {
            throw exception;
        }
        return accepted;
    }

    async Task<bool> PromptOnUI() {
        Window dialog = new() {
            Width = 580,
            Height = 580,
            MinWidth = 420,
            MinHeight = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#696969"))
        };

        Button accept = new() {
            Content = acceptText,
            Width = 100
        };
        Button cancel = new() {
            Content = cancelText,
            Width = 100
        };
        accept.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);

        dialog.Content = new Border {
            Background = new SolidColorBrush(Color.Parse("#222222")),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(10),
            Padding = new Thickness(14),
            Child = new Grid {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                RowSpacing = 10,
                Children = {
                    new TextBlock {
                        Text = description,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.White
                    },
                    new ScrollViewer {
                        Content = new TextBlock {
                            Text = licence,
                            FontFamily = new FontFamily("Consolas, Menlo, monospace"),
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = Brushes.White
                        }
                    },
                    new StackPanel {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = {
                            accept,
                            cancel
                        }
                    }
                }
            }
        };

        Grid grid = (Grid)((Border)dialog.Content).Child;
        Grid.SetRow(grid.Children[1], 1);
        Grid.SetRow(grid.Children[2], 2);
        return await dialog.ShowDialog<bool>(owner);
    }
}
