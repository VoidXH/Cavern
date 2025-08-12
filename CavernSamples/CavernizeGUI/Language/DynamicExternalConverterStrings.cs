using System.Windows;

using Cavernize.Logic.Language;

namespace CavernizeGUI.Language {
    /// <summary>
    /// Reads the <see cref="ExternalConverterStrings"/> from Cavernize GUI's localized resources.
    /// </summary>
    /// <param name="source">Localized resources for <see cref="ExternalConverterStrings"/></param>
    public class DynamicExternalConverterStrings(ResourceDictionary source) : ExternalConverterStrings {
        /// <inheritdoc/>
        public override string LicenceNeeded => (string)source["LicNe"];

        /// <inheritdoc/>
        public override string LicenceFetch => (string)source["LicFe"];

        /// <inheritdoc/>
        public override string LicenceFail => (string)source["LicFa"];

        /// <inheritdoc/>
        public override string WaitingUserAccept => (string)source["LicWa"];

        /// <inheritdoc/>
        public override string UserCancelled => (string)source["LicCa"];

        /// <inheritdoc/>
        public override string Downloading => (string)source["ExDow"];

        /// <inheritdoc/>
        public override string Extracting => (string)source["ExExt"];

        /// <inheritdoc/>
        public override string ExtractingBitstream => (string)source["ExRaw"];

        /// <inheritdoc/>
        public override string Converting => (string)source["ConvW"];

        /// <inheritdoc/>
        public override string NetworkError => (string)source["DlErr"];
    }
}
