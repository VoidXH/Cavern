using System.Collections;
using System.Globalization;

namespace Cavern.MAUI.Graphics;

/// <summary>
/// Alternates row colors in a list of grids for better readability.
/// </summary>
public class ZebraStripes : IValueConverter {
    /// <summary>
    /// Background color of even indexed rows.
    /// </summary>
    public Color EvenRow { get; set; } = Colors.Transparent;
    /// <summary>
    /// Background color of odd indexed rows.
    /// </summary>
    public Color OddRow { get; set; } = Colors.LightGray;

    /// <inheritdoc/>
    public object Convert(object value, Type _, object parameter, CultureInfo __) {
        if (value == null) {
            return EvenRow;
        }

        CollectionView collectionView = parameter as CollectionView;
        if (collectionView?.ItemsSource is IList items) {
            int index = items.IndexOf(value);
            return (index & 1) == 0 ? EvenRow : OddRow;
        }

        return EvenRow;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}