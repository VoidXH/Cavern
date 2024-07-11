using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Controls;

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
        /// The object to edit this <see cref="property"/> of.
        /// </summary>
        protected readonly object source = source;

        /// <summary>
        /// The property of the <see cref="source"/> to edit.
        /// </summary>
        protected readonly PropertyInfo property = property;

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
        protected string value = property.GetValue(source, null)?.ToString() ?? "null";

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
    /// An editable property that uses an external handler or dialog to update the <paramref name="property"/>
    /// of the <paramref name="source"/>.
    /// </summary>
    public class ExternallyEditablePropertyDisplay : PropertyDisplay {
        /// <summary>
        /// Passes the value of the property in the source object for editing externally.
        /// </summary>
        readonly Action<object> editCallback;

        /// <summary>
        /// An editable property that uses an external handler or dialog to update the <paramref name="property"/>
        /// of the <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The object to edit this <paramref name="property"/> of</param>
        /// <param name="property">The property of the <paramref name="source"/> to edit</param>
        /// <param name="editCallback">Passes the value of the <paramref name="property"/> in the <paramref name="source"/> object
        /// for editing externally</param>
        public ExternallyEditablePropertyDisplay(object source, PropertyInfo property, Action<object> editCallback) :
            base(source, property, null, null) {
            this.editCallback = editCallback;
            value = "...";
        }

        /// <summary>
        /// Call the <see cref="editCallback"/> with the property value to edit.
        /// </summary>
        public void Edit() => editCallback(property.GetValue(source));
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
        /// <param name="customFields">For these types, ... will be displayed for value, and an action calls back with the value
        /// for modification</param>
        public ObjectToDataGrid(object source, Action successCallback, Action<Exception> failureCallback,
            params(Type type, Action<object> editor)[] customFields) {
            PropertyInfo[] properties = source.GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++) {
                PropertyInfo property = properties[i];
                if (property.GetCustomAttribute<IgnoreDataMemberAttribute>() != null) {
                    continue;
                }

                if (property.SetMethod != null && property.SetMethod.IsPublic) {
                    (Type type, Action<object> editor) = customFields.FirstOrDefault(x => x.type == property.PropertyType);
                    if (editor == null) {
                        Add(new PropertyDisplay(source, property, successCallback, failureCallback));
                    } else {
                        Add(new ExternallyEditablePropertyDisplay(source, property, editor));
                    }
                }
            }
        }

        /// <summary>
        /// Attach this function to the <see cref="DataGrid"/>'s corresponding event to support
        /// <see cref="ExternallyEditablePropertyDisplay"/>s.
        /// </summary>
        public void BeginningEdit(object _, DataGridBeginningEditEventArgs e) {
            if (this[e.Row.GetIndex()] is ExternallyEditablePropertyDisplay editable) {
                editable.Edit();
                e.EditingEventArgs.Handled = true;
                e.Cancel = true;
            }
        }
    }
}