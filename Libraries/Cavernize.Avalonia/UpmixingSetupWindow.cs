using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

using Cavern.CavernSettings;

namespace Cavernize.Avalonia;

public sealed class UpmixingSetupWindow : Window {
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
    readonly UpmixingSettings settings;

    public bool Accepted { get; private set; }

    public UpmixingSetupWindow(UpmixingSettings settings, string title, string matrixUpmixing, string matrixUpmixingTip,
        string cavernizeUpmixing, string cavernizeUpmixingTip, string effectText, string smoothnessText,
        string resetText, string okText, string cancelText) {
        this.settings = settings;
        Title = title;
        Width = 380;
        Height = 235;
        MinWidth = 380;
        MinHeight = 235;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#696969"));

        matrixUpmix.Content = matrixUpmixing;
        cavernize.Content = cavernizeUpmixing;
        matrixUpmix.IsChecked = settings.MatrixUpmixing;
        cavernize.IsChecked = settings.Cavernize;
        effect.Value = settings.Effect * 100;
        smoothness.Value = settings.Smoothness * 100;
        ToolTip.SetTip(matrixUpmix, matrixUpmixingTip);
        ToolTip.SetTip(cavernize, cavernizeUpmixingTip);

        Grid effectRow = new() {
            ColumnDefinitions = new ColumnDefinitions("160,*"),
            Children = {
                new TextBlock {
                    Text = effectText,
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
                    Text = smoothnessText,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White
                },
                smoothness
            }
        };
        Grid.SetColumn(smoothness, 1);

        Button reset = new() {
            Content = resetText,
            Width = 82
        };
        Button ok = new() {
            Content = okText,
            Width = 82
        };
        Button cancel = new() {
            Content = cancelText,
            Width = 82
        };
        reset.Click += (_, _) => Reset();
        ok.Click += (_, _) => {
            Apply();
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

    void Apply() {
        settings.MatrixUpmixing = matrixUpmix.IsChecked == true;
        settings.Cavernize = cavernize.IsChecked == true;
        settings.Effect = (float)(effect.Value * .01);
        settings.Smoothness = (float)(smoothness.Value * .01);
    }

    void Reset() {
        matrixUpmix.IsChecked = false;
        cavernize.IsChecked = false;
        effect.Value = 75;
        smoothness.Value = 80;
    }
}
