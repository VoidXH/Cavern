using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

using FilterStudio;

namespace VoidX.WPF {
    /// <summary>
    /// Displays a <see cref="PropertyInfo"/> as a row of a DataGrid.
    /// </summary>
    /// <param name="source">The object to edit this <paramref name="property"/> of</param>
    /// <param name="property">The property of the <paramref name="source"/> to edit</param>
    /// <param name="successCallback">Call this function when the <paramref name="property"/> was changed</param>
    /// <param name="failureCallback">Call this function when the <paramref name="property"/> wasn't changed</param>
    public class PropertyDisplay(object source, PropertyInfo property, Action successCallback, Action<Exception> failureCallback) {
        /// <summary>
        /// Name of the property, or the display name if a <see cref="DisplayNameAttribute"/> is present.
        /// </summary>
        public string Property { get; } = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? property.Name;

        /// <summary>
        /// Current value of the property as a string.
        /// </summary>
        public string Value {
            get => value;
            set {
                this.value = value;
                try {
                    if (typeParsers.TryGetValue(property.PropertyType, out var parser)) {
                        property.SetValue(source, parser(value));
                    } else {
                        throw new UneditableTypeException();
                    }
                    successCallback();
                } catch (Exception e) {
                    failureCallback(e);
                }
            }
        }
        string value = property.GetValue(source, null)?.ToString() ?? "null";

        /// <summary>
        /// Quick access to the parsers of supported types.
        /// </summary>
        static readonly Dictionary<Type, Func<string, object>> typeParsers = new() {
            { typeof(bool), value => bool.Parse(value) },
            { typeof(byte), value => byte.Parse(value) },
            { typeof(char), value => char.Parse(value) },
            { typeof(DateTime), value => DateTime.Parse(value) },
            { typeof(decimal), value => decimal.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture) },
            { typeof(double), value => double.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture) },
            { typeof(float), value => float.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture) },
            { typeof(int), value => int.Parse(value) },
            { typeof(long), value => long.Parse(value) },
            { typeof(sbyte), value => sbyte.Parse(value) },
            { typeof(short), value => short.Parse(value) },
            { typeof(string), value => value },
            { typeof(uint), value => uint.Parse(value) },
            { typeof(ulong), value => ulong.Parse(value) },
            { typeof(ushort), value => ushort.Parse(value) }
        };
    }

    /// <summary>
    /// Makes the properties of classes available for editing as key-value string pairs.
    /// </summary>
    public class ObjectToDataGrid : List<PropertyDisplay> {
        /// <summary>
        /// Makes the properties of classes available for editing as key-value string pairs.
        /// </summary>
        /// <param name="source">The object to edit</param>
        /// <param name="successCallback">Call this function when a property was changed</param>
        /// <param name="failureCallback">Call this function when a property wasn't changed</param>
        public ObjectToDataGrid(object source, Action successCallback, Action<Exception> failureCallback) {
            PropertyInfo[] properties = source.GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++) {
                PropertyInfo property = properties[i];
                if (property.SetMethod != null && property.SetMethod.IsPublic) {
                    Add(new PropertyDisplay(source, property, successCallback, failureCallback));
                }
            }
        }
    }
}