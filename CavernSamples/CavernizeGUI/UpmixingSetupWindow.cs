using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace CavernizeGUI;

sealed class UpmixingSetupWindow : Window {
    readonly CheckBox matrixUpmix = new() {
        Foreground = Brushes.White
    };
    readonly CheckBox cavernize = new() {
        Foreground = Brushes.White
    };
    readonly Slider effect = new() {
        Minimum = 0,
        Maximum = 100
    };
    readonly Slider smoothness = new() {
        Minimum = 0,
        Maximum = 100
    };

    public bool Accepted { get; private set; }

    public UpmixingSetupWindow(MainViewModel viewModel) {
        Title = viewModel.Text("UpmTi");
        Width = 380;
        Height = 235;
        MinWidth = 380;
        MinHeight = 235;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#696969"));

        matrixUpmix.Content = viewModel.Text("Upm71");
        cavernize.Content = viewModel.Text("UpmNs");
        matrixUpmix.IsChecked = viewModel.MatrixUpmixing;
        cavernize.IsChecked = viewModel.CavernizeUpmixing;
        effect.Value = viewModel.UpmixingEffect * 100;
        smoothness.Value = viewModel.UpmixingSmoothness * 100;
        ToolTip.SetTip(matrixUpmix, viewModel.Text("Upm71T"));
        ToolTip.SetTip(cavernize, viewModel.Text("UpmNsT"));

        Grid effectRow = new() {
            ColumnDefinitions = new ColumnDefinitions("160,*"),
            Children = {
                new TextBlock {
                    Text = viewModel.Text("UpmEf"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White
                },
                effect
            }
        };
        Grid.SetColumn(effect, 1);

        Grid smoothnessRow = new() {
            ColumnDefinitions = new ColumnDefinitions("160,*"),
            Children = {
                new TextBlock {
                    Text = viewModel.Text("UpmSm"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White
                },
                smoothness
            }
        };
        Grid.SetColumn(smoothness, 1);

        Button reset = new() {
            Content = viewModel.Text("Reset"),
            Width = 82
        };
        Button ok = new() {
            Content = viewModel.Text("OK"),
            Width = 82
        };
        Button cancel = new() {
            Content = viewModel.Text("Cancel"),
            Width = 82
        };
        reset.Click += (_, _) => Reset();
        ok.Click += (_, _) => {
            Accepted = true;
            Close();
        };
        cancel.Click += (_, _) => Close();
        Grid buttons = new() {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            ColumnSpacing = 8,
            VerticalAlignment = VerticalAlignment.Bottom,
            Children = {
                reset,
                ok,
                cancel
            }
        };
        Grid.SetColumn(ok, 2);
        Grid.SetColumn(cancel, 3);

        Content = new Border {
            Background = new SolidColorBrush(Color.Parse("#222222")),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(10),
            Padding = new Thickness(14),
            Child = new Grid {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,*"),
                RowSpacing = 10,
                Children = {
                    matrixUpmix,
                    cavernize,
                    effectRow,
                    smoothnessRow,
                    buttons
                }
            }
        };
        Grid.SetRow(cavernize, 1);
        Grid.SetRow(effectRow, 2);
        Grid.SetRow(smoothnessRow, 3);
        Grid.SetRow(buttons, 4);
    }

    public void ApplyTo(MainViewModel viewModel) =>
        viewModel.ApplyUpmixingSettings(matrixUpmix.IsChecked == true, cavernize.IsChecked == true,
            (float)(effect.Value * .01), (float)(smoothness.Value * .01));

    void Reset() {
        matrixUpmix.IsChecked = false;
        cavernize.IsChecked = false;
        effect.Value = 75;
        smoothness.Value = 80;
    }
}
