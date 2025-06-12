using System.Windows;

using Cavernize.Logic.Language;

namespace CavernizeGUI.Language {
    /// <summary>
    /// Reads the <see cref="RenderReportStrings"/> from Cavernize GUI's localized resources.
    /// </summary>
    /// <param name="source">Localized resources for <see cref="RenderReportStrings"/></param>
    public class DynamicRenderReportStrings(ResourceDictionary source) : RenderReportStrings {
        /// <inheritdoc/>
        public override string Default => (string)source["Defau"];

        /// <inheritdoc/>
        public override string ActualBeds => (string)source["ABeds"];

        /// <inheritdoc/>
        public override string ActualObjects => (string)source["AObjs"];

        /// <inheritdoc/>
        public override string FakeTargets => (string)source["FakeT"];

        /// <inheritdoc/>
        public override string PeakGain => (string)source["PeaGa"];

        /// <inheritdoc/>
        public override string RMSGain => (string)source["RMSGa"];

        /// <inheritdoc/>
        public override string Macrodynamics => (string)source["MacDy"];

        /// <inheritdoc/>
        public override string Microdynamics => (string)source["MicDy"];

        /// <inheritdoc/>
        public override string NoLFE => (string)source["NoLFE"];

        /// <inheritdoc/>
        public override string LFEPeak => (string)source["PeaLF"];

        /// <inheritdoc/>
        public override string LFERMS => (string)source["RMSLF"];

        /// <inheritdoc/>
        public override string LFEMacrodynamics => (string)source["MacLF"];

        /// <inheritdoc/>
        public override string LFEMicrodynamics => (string)source["MicLF"];

        /// <inheritdoc/>
        public override string ChestSlam => (string)source["CheSl"];

        /// <inheritdoc/>
        public override string SurroundUsage => (string)source["SurUs"];

        /// <inheritdoc/>
        public override string HeightUsage => (string)source["HeiUs"];

        /// <inheritdoc/>
        protected override string[] GetGrades() => [
            (string)source["Grad0"],
            (string)source["Grad1"],
            (string)source["Grad2"],
            (string)source["Grad3"],
            (string)source["Grad4"],
            (string)source["Grad5"]
        ];
    }
}
