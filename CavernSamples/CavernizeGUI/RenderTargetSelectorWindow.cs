using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

using Cavernize.Logic.Models.RenderTargets;

namespace CavernizeGUI;

sealed class RenderTargetSelectorWindow : Window {
    public RenderTargetSelectorWindow(MainViewModel viewModel) {
        Title = viewModel.RenderTargetLabel.TrimEnd(':');
        Width = 540;
        Height = 390;
        MinWidth = 540;
        MinHeight = 390;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#424242"));

        Grid content = new() {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,*"),
            ColumnSpacing = 12,
            RowSpacing = 10,
            Margin = new Thickness(12)
        };

        AddColumn(content, 0, viewModel.RenderTargetSelectorText("PCRea"),
            viewModel.RenderTargets.Where(target => target.OutputChannels <= 8 &&
                (target is not DownmixedRenderTarget downmixed || !downmixed.IsMatrixWired)), viewModel.SelectedRenderTarget);
        AddColumn(content, 1, viewModel.RenderTargetSelectorText("Matri"),
            viewModel.RenderTargets.Where(target => target.OutputChannels <= 8 &&
                target is DownmixedRenderTarget downmixed && downmixed.IsMatrixWired), viewModel.SelectedRenderTarget);
        AddColumn(content, 2, viewModel.RenderTargetSelectorText("MulCH"),
            viewModel.RenderTargets.Where(target => target.OutputChannels > 8), viewModel.SelectedRenderTarget);

        Content = new Border {
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1),
            Child = content
        };
    }

    void AddColumn(Grid content, int column, string header, IEnumerable<RenderTarget> targets, RenderTarget selected) {
        TextBlock title = new() {
            Text = header,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 6)
        };
        Grid.SetColumn(title, column);
        content.Children.Add(title);

        StackPanel list = new() {
            Spacing = 5
        };
        foreach (RenderTarget target in targets) {
            RadioButton button = new() {
                Content = target.Name,
                Foreground = Brushes.White,
                IsChecked = target == selected,
                GroupName = "RenderTargets",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            button.Click += (_, _) => Close(target);
            list.Children.Add(button);
        }

        ScrollViewer scroll = new() {
            Content = list
        };
        Grid.SetColumn(scroll, column);
        Grid.SetRow(scroll, 1);
        content.Children.Add(scroll);
    }
}
