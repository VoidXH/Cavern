using System;
using System.Windows;

using Cavern.WPF.BaseClasses;

namespace FilterStudio.Windows {
    /// <summary>
    /// Dialog for showing an editable name for an object.
    /// </summary>
    public partial class RenameDialog : OkCancelDialog {
        /// <summary>
        /// The new name the user entered.
        /// </summary>
        public string NewName => name.Text;

        /// <summary>
        /// Dialog for showing an editable name for an object.
        /// </summary>
        public RenameDialog(string oldName) {
            ResourceDictionary language = Consts.Language.GetRenameDialogStrings();
            Resources.MergedDictionaries.Add(new() {
                Source = new Uri($";component/Resources/Styles.xaml", UriKind.RelativeOrAbsolute)
            });
            Resources.MergedDictionaries.Add(language);
            Resources.MergedDictionaries.Add(Cavern.WPF.Consts.Language.GetCommonStrings());
            InitializeComponent();
            description.Text = string.Format((string)language["DNewN"], oldName);
            name.Text = oldName;
        }
    }
}