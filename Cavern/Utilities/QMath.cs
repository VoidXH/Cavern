using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Two plus two is four, minus one, that's three, quick maths.
    /// </summary>
    public static partial class QMath {
        /// <summary>
        /// Clamps the value between 0 and 1.
        /// </summary>
        public static float Clamp01(float x) {
            if (x < 0) {
                return 0;
            }
            if (x > 1) {
                return 1;
            }
            return x;
        }

        /// <summary>
        /// Convert decibels to voltage gain.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DbToGain(double gain) => Math.Pow(10, gain * .05);

        /// <summary>
        /// Convert decibels to voltage gain.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DbToGain(float gain) => MathF.Pow(10, gain * .05f);

        /// <summary>
        /// Convert voltage gain to decibels.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GainToDb(double gain) => 20 * Math.Log10(gain);

        /// <summary>
        /// Convert voltage gain to decibels.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GainToDb(float gain) => 20 * MathF.Log10(gain);

        /// <summary>
        /// Counts the leading zeros in a byte. The byte is contained in an integer, but in byte limits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZerosInByte(int x) => 7 - BitsAfterMSB(x);

        /// <summary>
        /// Counts the leading zeros in an integer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZeros(int x) => 31 - BitsAfterMSB(x);

        /// <summary>
        /// Parse a double value regardless of the system's culture.
        /// </summary>
        public static double ParseDouble(string s) {
            char separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
            if (s.IndexOf(separator) >= 0) {
                return Convert.ToDouble(s);
            }
            return Convert.ToDouble(NumberToInvariant(s), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parse a float value regardless of the system's culture.
        /// </summary>
        public static float ParseFloat(string s) {
            char separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
            if (s.IndexOf(separator) >= 0) {
                return Convert.ToSingle(s);
            }
            return Convert.ToSingle(NumberToInvariant(s), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Round a floating point number to integer correctly.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundToInt(double value) => (int)(value + .5f);

        /// <summary>
        /// Round a floating point number to integer correctly.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundToInt(float value) => (int)(value + .5f);

        /// <summary>
        /// Checks if the two numbers have the same sign.
        /// </summary>
        /// <remarks>This function does not handle 0, 0 correctly for optimization purposes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SignCompare(float a, float b) => a * b > 0;

        /// <summary>
        /// Limit the number of decimal digits when a number is converted to string.
        /// </summary>
        public static string ToStringLimitDecimals(double value, int decimals) =>
            value.ToString("0." + new string('#', decimals), CultureInfo.InvariantCulture);

        /// <summary>
        /// Try to parse a double value regardless of the system's culture.
        /// </summary>
        public static bool TryParseDouble(string from, out double num) =>
            double.TryParse(NumberToInvariant(from), NumberStyles.Any, CultureInfo.InvariantCulture, out num);

        /// <summary>
        /// Try to parse a floating-point value regardless of the system's culture.
        /// </summary>
        public static bool TryParseFloat(string from, out float num) =>
            float.TryParse(NumberToInvariant(from), NumberStyles.Any, CultureInfo.InvariantCulture, out num);

        /// <summary>
        /// Convert many number representations to <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        static string NumberToInvariant(string from) {
            int comma = from.IndexOf(',');
            if (comma == -1) {
                return from;
            }

            int dot = from.IndexOf('.');
            if (dot == -1) {
                return from.Replace(',', '.');
            } else {
                return from.Replace(",", string.Empty);
            }
        }
    }
}
