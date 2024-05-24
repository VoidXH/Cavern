using System.Globalization;

namespace Cavern.Utilities {
    /// <summary>
    /// Adds a version of <see cref="object.ToString"/> that can be translated.
    /// </summary>
    public interface ILocalizableToString {
        /// <summary>
        /// Returns a string that represents the current objects in the passed language.
        /// </summary>
        string ToString(CultureInfo culture);
    }
}