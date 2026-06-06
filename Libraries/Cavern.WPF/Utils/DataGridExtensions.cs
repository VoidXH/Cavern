using System.Data;
using System.Windows.Controls;

namespace Cavern.WPF.Utils;

/// <summary>
/// Special operations on <see cref="DataGrid"/>s.
/// </summary>
public static class DataGridExtensions {
    /// <summary>
    /// Display a 2D matrix in a DataGrid. A number <paramref name="format"/>ting can be specified.
    /// </summary>
    public static void DisplayMatrix(this DataGrid on, float[][] data, string columnPrefix, string format = null) {
        DataTable dataTable = new DataTable();
        if (data == null || data.Length == 0) {
            on.ItemsSource = dataTable.DefaultView;
            return;
        }

        int numColumns = 0;
        foreach (float[] row in data) {
            if (row.Length > numColumns) {
                numColumns = row.Length;
            }
        }

        for (int i = 0; i < numColumns; i++) {
            dataTable.Columns.Add($"{columnPrefix} {i}", typeof(float));
        }

        foreach (float[] row in data) {
            object[] rowData = new object[numColumns];
            if (format != null) {
                for (int j = 0; j < row.Length; j++) {
                    rowData[j] = row[j].ToString(format);
                }
            } else {
                for (int j = 0; j < row.Length; j++) {
                    rowData[j] = row[j];
                }
            }
            dataTable.Rows.Add(rowData);
        }

        on.AutoGenerateColumns = true;
        on.HeadersVisibility = DataGridHeadersVisibility.All;
        on.IsReadOnly = true;
        on.ItemsSource = dataTable.DefaultView;
    }
}
