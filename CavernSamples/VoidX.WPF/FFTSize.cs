using System;
using System.Windows;

namespace VoidX.WPF {
    /// <summary>
    /// FFT size (numbers that are a power of 2) selector control with limits.
    /// </summary>
    public partial class FFTSize : NumericUpDown {
        /// <inheritdoc/>
        protected override void CheckValue() {
            value = (int)Math.Pow(2, Math.Round(Math.Log(value, 2)));
            base.CheckValue();
        }

        /// <inheritdoc/>
        protected override void Increase(object sender, RoutedEventArgs e) => Value = (int)Math.Pow(2, Math.Log(Value, 2) + 1);

        /// <inheritdoc/>
        protected override void Decrease(object sender, RoutedEventArgs e) => Value = (int)Math.Pow(2, Math.Log(Value, 2) - 1);
    }
}