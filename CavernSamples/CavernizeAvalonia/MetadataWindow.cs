using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

using Cavern.Format.Common;

namespace CavernizeAvalonia;

sealed class MetadataWindow : Window {
    public MetadataWindow(ReadableMetadata metadata, string title) {
        Title = title;
        Width = 760;
        Height = 520;
        MinWidth = 520;
        MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#696969"));

        StackPanel headers = new() {
            Spacing = 10
        };
        foreach (ReadableMetadataHeader header in metadata.Headers) {
            headers.Children.Add(CreateHeader(header));
        }

        Content = new Border {
            Background = new SolidColorBrush(Color.Parse("#222222")),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(10),
            Padding = new Thickness(14),
            Child = new ScrollViewer {
                Content = headers
            }
        };
    }

    static Control CreateHeader(ReadableMetadataHeader header) {
        StackPanel fields = new() {
            Spacing = 3
        };
        fields.Children.Add(new TextBlock {
            Text = $"{header.Name} ({header.Fields.Count})",
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 4)
        });

        foreach (ReadableMetadataField field in header.Fields) {
            fields.Children.Add(new Grid {
                ColumnDefinitions = new ColumnDefinitions("160,160,*"),
                ColumnSpacing = 8,
                Children = {
                    Cell(field.Name, FontWeight.Normal),
                    Cell(field.Value?.ToString(), FontWeight.Normal, 1),
                    Cell(field.Description, FontWeight.Normal, 2)
                }
            });
        }

        return new Border {
            BorderBrush = new SolidColorBrush(Color.Parse("#555555")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8),
            Child = fields
        };
    }

    static TextBlock Cell(string text, FontWeight weight, int column = 0) {
        TextBlock result = new() {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White,
            FontWeight = weight
        };
        Grid.SetColumn(result, column);
        return result;
    }
}
