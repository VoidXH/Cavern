using Cavern.Utilities;
using Cavern.WPF.BaseClasses;

namespace Cavern.WPF {
    /// <summary>
    /// Window displaying a licence that needs to be accepted before continuing.
    /// </summary>
    public partial class LicenceWindow : OkCancelDialog, ILicence {
        /// <summary>
        /// Window displaying a licence that needs to be accepted before continuing.
        /// </summary>
        public LicenceWindow() {
            Resources.MergedDictionaries.Add(Consts.Language.GetCommonStrings());
            InitializeComponent();
        }

        /// <summary>
        /// Window displaying a licence that needs to be accepted before continuing.
        /// </summary>
        public LicenceWindow(string description) : this() => this.description.Text = description;

        /// <summary>
        /// Window displaying a licence that needs to be accepted before continuing.
        /// </summary>
        public LicenceWindow(string description, string licence) : this(description) => SetLicenceText(licence);

        /// <inheritdoc/>
        public void SetDescription(string description) => this.description.Text = description;

        /// <inheritdoc/>
        public void SetLicenceText(string licence) => this.licence.Text = licence;

        /// <inheritdoc/>
        public bool Prompt() => ShowDialog().Value;
    }
}
