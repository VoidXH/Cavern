namespace Cavernize.Logic.Language {
    /// <summary>
    /// Strings used for generating a post-render report. Override to provide custom translations. Summaries are the default translations.
    /// </summary>
    public class RenderReportStrings {
        /// <summary>
        /// Display this instead of the report when it's available yet.
        /// </summary>
        public virtual string Default => "After rendering has finished, more track information will appear here, like true object usage statistics.";

        /// <summary>
        /// Actually present bed channels
        /// </summary>
        public virtual string ActualBeds => "Actually present bed channels";

        /// <summary>
        /// Actually present dynamic objects
        /// </summary>
        public virtual string ActualObjects => "Actually present dynamic objects";

        /// <summary>
        /// Unused (fake) rendering targets
        /// </summary>
        public virtual string FakeTargets => "Unused (fake) rendering targets";

        /// <summary>
        /// Peak audio frame level
        /// </summary>
        public virtual string PeakGain => "Peak audio frame level";

        /// <summary>
        /// RMS content level
        /// </summary>
        public virtual string RMSGain => "RMS content level";

        /// <summary>
        /// Macrodynamics
        /// </summary>
        public virtual string Macrodynamics => "Macrodynamics";

        /// <summary>
        /// Microdynamics
        /// </summary>
        public virtual string Microdynamics => "Microdynamics";

        /// <summary>
        /// Display this when the LFE cannot be graded.
        /// </summary>
        public virtual string NoLFE => "The LFE channel was either missing from the source, unused, or not rendered.";

        /// <summary>
        /// Peak LFE level
        /// </summary>
        public virtual string LFEPeak => "Peak LFE level";

        /// <summary>
        /// RMS LFE level
        /// </summary>
        public virtual string LFERMS => "RMS LFE level";

        /// <summary>
        /// LFE macrodynamics
        /// </summary>
        public virtual string LFEMacrodynamics => "LFE macrodynamics";

        /// <summary>
        /// LFE microdynamics
        /// </summary>
        public virtual string LFEMicrodynamics => "LFE microdynamics";

        /// <summary>
        /// Chest slam grade
        /// </summary>
        public virtual string ChestSlam => "Chest slam grade";

        /// <summary>
        /// Surround usage
        /// </summary>
        public virtual string SurroundUsage => "Surround usage";

        /// <summary>
        /// Height usage
        /// </summary>
        public virtual string HeightUsage => "Height usage";

        /// <summary>
        /// 6 grades from best to worst.
        /// </summary>
        public string[] Grades => grades ??= GetGrades();

        /// <summary>
        /// Cached result of <see cref="Grades"/>.
        /// </summary>
        string[] grades;

        /// <summary>
        /// Store the supported 6 grades from best to worst.
        /// </summary>
        protected virtual string[] GetGrades() => [
            "A+",
            "A",
            "B",
            "C",
            "D",
            "F"
        ];
    }
}
