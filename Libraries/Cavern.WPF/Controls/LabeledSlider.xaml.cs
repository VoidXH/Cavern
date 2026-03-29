using System.Windows;
using System.Windows.Controls;

namespace Cavern.WPF.Controls;

/// <summary>
/// Has a text and value-displaying label for a slider.
/// </summary>
public partial class LabeledSlider : UserControl {
    /// <summary>
    /// The text of the label above the slider.
    /// </summary>
    public string Label {
        get => label.Text;
        set => label.Text = value;
    }

    /// <summary>
    /// Which string formatting shall be used for the value display.
    /// </summary>
    public string ValueFormat {
        get => valueFormat;
        set {
            valueFormat = value;
            SliderChanged(null, null);
        }
    }
    string valueFormat = "0.##";

    /// <summary>
    /// The minimum value of the slider.
    /// </summary>
    public double Minimum {
        get => slider.Minimum;
        set => slider.Minimum = value;
    }

    /// <summary>
    /// The current value of the slider.
    /// </summary>
    public double Value {
        get => slider.Value;
        set => slider.Value = value;
    }

    /// <summary>
    /// The maximum value of the slider.
    /// </summary>
    public double Maximum {
        get => slider.Maximum;
        set => slider.Maximum = value;
    }

    /// <summary>
    /// In what steps the slider can be changed when stepping.
    /// </summary>
    public double SmallChange {
        get => slider.SmallChange;
        set => slider.SmallChange = value;
    }

    /// <summary>
    /// In what steps the slider can be changed when seeking.
    /// </summary>
    public double LargeChange {
        get => slider.LargeChange;
        set => slider.LargeChange = value;
    }

    /// <summary>
    /// The selected value of the slider, rounded to the nearest integer.
    /// </summary>
    public int IntegerValue => (int)(slider.Value + .5f);

    /// <summary>
    /// Called when the slider value changes.
    /// </summary>
    public event RoutedPropertyChangedEventHandler<double> ValueChanged;

    /// <summary>
    /// Has a text and value-displaying label for a slider.
    /// </summary>
    public LabeledSlider() => InitializeComponent();

    /// <summary>
    /// Update the label when the slider changes.
    /// </summary>
    void SliderChanged(object _, RoutedPropertyChangedEventArgs<double> e) {
        value.Text = slider.Value.ToString(valueFormat);
        ValueChanged?.Invoke(this, e);
    }
}
