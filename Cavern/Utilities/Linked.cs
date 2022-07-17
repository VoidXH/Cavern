using System;
using System.Reflection;

namespace Cavern.Utilities {
    /// <summary>
    /// Marks a relationship between two arrays that are not merged in a struct for better performance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class Linked : Attribute {
        /// <summary>
        /// The other field linked with this field.
        /// </summary>
        readonly string targetField;

        /// <summary>
        /// Marks a relationship between two arrays that are not merged in a struct for better performance.
        /// </summary>
        public Linked(string targetField) => this.targetField = targetField;

        /// <summary>
        /// Checks if a linking is valid in an object.
        /// </summary>
        public bool IsValid(object value) {
            FieldInfo[] fields = value.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic);
            for (int field = 0; field < fields.Length; ++field) {
                if (fields[field].Name.Equals(targetField)) {
                    return true;
                }
            }
            return false;
        }
    }
}