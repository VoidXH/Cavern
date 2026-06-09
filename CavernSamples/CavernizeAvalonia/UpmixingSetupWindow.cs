using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace CavernizeAvalonia;

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
        Title = viewModel.Text("UpmTi", "Upmixing Setup");
        Width = 380;
        Height = 235;
        MinWidth = 380;
        MinHeight = 235;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#696969"));

        matrixUpmix.Content = viewModel.Text("Upm71", "Fill 7.1");
        cavernize.Content = viewModel.Text("UpmNs", "Upconvert non-spatial content");
        matrixUpmix.IsChecked = viewModel.MatrixUpmixing;
        cavernize.IsChecked = viewModel.CavernizeUpmixing;
        effect.Value = viewModel.UpmixingEffect * 100;
        smoothness.Value = viewModel.UpmixingSmoothness * 100;
        ToolTip.SetTip(matrixUpmix, "Use a matrix upmixer to create the full 7.1 bed for legacy channel-based content.");
        ToolTip.SetTip(cavernize, "Tries to recreate height information for regular content up to 7.1.");

        Grid effectRow = new() {
            ColumnDefinitions = new ColumnDefinitions("160,*"),
            Children = {
                new TextBlock {
                    Text = viewModel.Text("UpmEf", "Upmixing effect:"),
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
                    Text = viewModel.Text("UpmSm", "Upmixing smoothness:"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White
                },
                smoothness
            }
        };
        Grid.SetColumn(smoothness, 1);

        Button reset = new() {
            Content = viewModel.Text("Reset", "Reset"),
            Width = 82
        };
        Button ok = new() {
            Content = "OK",
            Width = 82
        };
        Button cancel = new() {
            Content = viewModel.Text("Cancel", "Cancel"),
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
