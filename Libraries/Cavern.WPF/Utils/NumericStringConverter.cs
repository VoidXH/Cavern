using System.Globalization;
using System.Windows.Data;

namespace Cavern.WPF.Utils;

/// <summary>
/// Converts between a number and its string version, potentially in a text box.
/// Because input errors can happen, it doesn't throw exceptions, but reverts to a <see cref="defaultValue"/>.
/// </summary>
/// <param name="initialFill">The default value will be the first received value instead of the actual default of the converted type</param>
public class NumericStringConverter(bool initialFill) : IValueConverter {
    /// <summary>
    /// The value to fall back to when the input is invalid.
    /// </summary>
    object defaultValue;

    /// <summary>
    /// Creates a converter that fills its default value with the first value it receives.
    /// </summary>
    public NumericStringConverter() : this(true) { }

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (defaultValue is null) {
            if (initialFill) {
                defaultValue = value;
            } else {
                defaultValue = Array.CreateInstance(targetType, 1).GetValue(0); // default(targetType)
            }
        }
        return value?.ToString() ?? string.Empty;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        string text = value as string;
        if (targetType == typeof(double)) {
            return double.TryParse(text, NumberStyles.Any, culture, out double d) ? d : (defaultValue as double? ?? 0);
        } else if (targetType == typeof(float)) {
            return float.TryParse(text, NumberStyles.Any, culture, out float f) ? f : (defaultValue as float? ?? 0);
        } else if (targetType == typeof(int)) {
            return int.TryParse(text, NumberStyles.Any, culture, out int i) ? i : (defaultValue as int? ?? 0);
        } else {
            throw new NotImplementedException();
        }
    }
}