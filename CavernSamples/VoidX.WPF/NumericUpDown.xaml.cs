﻿using System.Windows;
using System.Windows.Controls;

namespace VoidX.WPF {
    /// <summary>
    /// Number selector control with limits.
    /// </summary>
    public partial class NumericUpDown : UserControl {
        /// <summary>
        /// Lower limit for the <see cref="Value"/> (inclusive).
        /// </summary>
        public int Minimum {
            get => minimum;
            set {
                minimum = value;
                CheckValue();
            }
        }
        int minimum = 1;

        /// <summary>
        /// Upper limit for the <see cref="Value"/> (inclusive).
        /// </summary>
        public int Maximum {
            get => maximum;
            set {
                maximum = value;
                CheckValue();
            }
        }
        int maximum = 100;

        /// <summary>
        /// User entered value.
        /// </summary>
        public int Value {
            get => value;
            set {
                valueField.Text = value.ToString();
                CheckValue();
            }
        }
        protected int value = 10;
        int oldValue;

        /// <summary>
        /// Number selector control with limits.
        /// </summary>
        public NumericUpDown() {
            oldValue = Value;
            InitializeComponent();
            valueField.Text = value.ToString();
        }

        /// <summary>
        /// Value changed delegate.
        /// </summary>
        public delegate void OnValueChanged(object sender, RoutedEventArgs e);

        /// <summary>
        /// Called when the <see cref="Value"/> is changed.
        /// </summary>
        public event OnValueChanged ValueChanged;

        /// <summary>
        /// Validate when the user enters a value.
        /// </summary>
        void ManualUpdate(object sender, TextChangedEventArgs e) {
            for (int i = 0; i < valueField.Text.Length; ++i) {
                if ((valueField.Text[i] < '0' || valueField.Text[i] > '9') && (i != 0 || valueField.Text[i] != '-')) {
                    valueField.Text = valueField.Text.Remove(i, 1);
                    ManualUpdate(sender, e);
                    return;
                }
            }
            if (valueField.Text.Length != 0 && !valueField.Text.Equals("-")) {
                value = int.Parse(valueField.Text);
                CheckValue();
            }
        }

        /// <summary>
        /// Clamp the value between limits.
        /// </summary>
        protected virtual void CheckValue() {
            if (Minimum <= Maximum) {
                if (value < Minimum) {
                    value = Minimum;
                    valueField.Text = Minimum.ToString();
                }
                if (value > Maximum) {
                    value = Maximum;
                    valueField.Text = Maximum.ToString();
                }
            }
            if (oldValue != value) {
                oldValue = value;
                ValueChanged?.Invoke(valueField, null);
            }
        }

        /// <summary>
        /// Increase the current value by one step.
        /// </summary>
        protected virtual void Increase(object sender, RoutedEventArgs e) => ++Value;

        /// <summary>
        /// Decrease the current value by one step.
        /// </summary>
        protected virtual void Decrease(object sender, RoutedEventArgs e) => --Value;
    }
}