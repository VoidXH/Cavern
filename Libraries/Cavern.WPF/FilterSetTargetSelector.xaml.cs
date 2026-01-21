using System.Windows;

using Cavern.Format.FilterSet;
using Cavern.WPF.BaseClasses;

namespace Cavern.WPF {
    /// <summary>
    /// User-selectable <see cref="FilterSetTarget"/> on a dropdown.
    /// </summary>
    public partial class FilterSetTargetSelector : OkCancelDialog {
        /// <summary>
        /// Displays a possible target device as a dropdown entry while the enum value can be quickly recovered.
        /// </summary>
        class SelectableFilterSet(FilterSetTarget device) {
            /// <summary>
            /// Target device.
            /// </summary>
            public FilterSetTarget Device => device;

            /// <summary>
            /// Parsed name of the target device or null if export is not supported.
            /// </summary>
            readonly string name = device.GetDeviceNameSafe();

            /// <inheritdoc/>
            public override string ToString() => name;
        }

        /// <summary>
        /// The user-selected target device after the dialog was closed.
        /// </summary>
        public FilterSetTarget Result { get; private set; }

        /// <summary>
        /// User-selectable <see cref="FilterSetTarget"/> on a dropdown.
        /// </summary>
        public FilterSetTargetSelector() {
            Resources.MergedDictionaries.Add(Consts.Language.GetCommonStrings());
            Resources.MergedDictionaries.Add(Consts.Language.GetFilterSetTargetSelectorStrings());
            InitializeComponent();
            FilterSetTarget[] targets = (FilterSetTarget[])Enum.GetValues(typeof(FilterSetTarget));
            device.ItemsSource = targets.Select(x => new SelectableFilterSet(x)).Where(x => x.ToString() != null);
            device.SelectedIndex = (int)FilterSetTarget.Generic;
        }

        /// <inheritdoc/>
        protected override void OK(object _, RoutedEventArgs e) {
            Result = ((SelectableFilterSet)device.SelectedItem).Device;
            base.OK(_, e);
        }
    }
}
