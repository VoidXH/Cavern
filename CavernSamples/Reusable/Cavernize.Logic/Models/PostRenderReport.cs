using System.Text;

using Cavern;
using Cavern.Channels;
using Cavern.Format.Renderers;
using Cavern.Utilities;

using Cavernize.Logic.Language;

namespace Cavernize.Logic.Models {
    /// <summary>
    /// Generates a report after a render has finished, scoring the audio in multiple stats.
    /// </summary>
    /// <param name="listener">The listener that played the content, where the objects are still attached</param>
    /// <param name="language">Source of localized strings in the report</param>
    public sealed class PostRenderReport(Listener listener, RenderReportStrings language) {
        /// <summary>
        /// Resulting post-render report from a <see cref="Generate"> call.
        /// </summary>
        public string Report {
            get => report ?? language.Default;
            private set => report = value;
        }
        string report;

        /// <summary>
        /// Give a grade to a metric. Starting from <paramref name="aPlus"/>, each grade has a range of <paramref name="stepsPerGrade"/>.
        /// </summary>
        /// <returns></returns>
        static int Grade(float value, float aPlus, float stepsPerGrade) {
            int grade = (int)((aPlus - value) / stepsPerGrade);
            if (grade < 0) {
                grade = 0;
            } else if (grade > 5) {
                grade = 5;
            }
            return grade;
        }

        /// <summary>
        /// Print a raw signal level value in decibels, without grading.
        /// </summary>
        static void ReportValue(StringBuilder builder, string metric, float signalValue) =>
            builder.Append(metric).Append(valueIs).AppendLine(QMath.GainToDb(signalValue).ToString("0.00 dB"));


        /// <summary>
        /// Extracts the <see cref="report"/> from a render to a text report.
        /// </summary>
        public void Generate(RenderStats stats) {
            int total = listener.ActiveSources.Count;
            int dynamic = total - stats.GetSemiStaticCount() - stats.GetSuperStaticCount();

            List<ReferenceChannel> channels =
                [.. stats.GetStaticOrSemiStaticPositions().Select(x => Renderer.ChannelFromPosition(x / Listener.EnvironmentSize))];
            channels.Sort();

            int unused = total - dynamic - channels.Count;
            if (unused < 0) {
                dynamic += unused;
                unused = 0;
            }

            StringBuilder builder = new();
            builder.Append(language.ActualBeds).Append(valueIs).Append(channels.Count);
            if (channels.Count != 0) {
                builder.Append(" (").Append(string.Join(", ", channels)).Append(')');
            }
            builder.AppendLine().Append(language.ActualObjects).Append(valueIs).AppendLine(dynamic.ToString())
                .Append(language.FakeTargets).Append(valueIs).AppendLine(unused.ToString());

            if (stats is RenderStatsEx statsEx) {
                float macrodynamics = QMath.GainToDb(statsEx.Macrodynamics),
                    microdynamics = QMath.GainToDb(statsEx.Microdynamics),
                    lfePeak = QMath.GainToDb(statsEx.LFELevelPeak),
                    lfeMacrodynamics = QMath.GainToDb(statsEx.LFEMacrodynamics),
                    lfeMicrodynamics = QMath.GainToDb(statsEx.LFEMicrodynamics),
                    surroundUsage = QMath.GainToDb(statsEx.RelativeSurroundLevel),
                    heightUsage = QMath.GainToDb(statsEx.RelativeHeightLevel);
                int macrodynamicsGrade = Grade(macrodynamics, 22, 3),
                    microdynamicsGrade = Grade(microdynamics, 40, 3),
                    lfePeakGrade = Grade(lfePeak, -3, 2),
                    lfeMacrodynamicsGrade = Grade(lfeMacrodynamics, 30, 3),
                    lfeMicrodynamicsGrade = Grade(lfeMicrodynamics, 60, 4),
                    surroundGrade = Grade(surroundUsage, 10, 1),
                    heightGrade = Grade(heightUsage, 10, 3);

                builder.AppendLine();
                ReportValue(builder, language.PeakGain, statsEx.FrameLevelPeak);
                ReportValue(builder, language.RMSGain, statsEx.FrameLevelRMS);
                ReportGrade(builder, language.Macrodynamics, macrodynamics, macrodynamicsGrade);
                ReportGrade(builder, language.Microdynamics, microdynamics, microdynamicsGrade);
                builder.AppendLine();

                if (statsEx.LFELevelPeak != 0) {
                    ReportGrade(builder, language.LFEPeak, lfePeak, lfePeakGrade);
                    ReportValue(builder, language.LFERMS, statsEx.LFELevelRMS);
                    ReportGrade(builder, language.LFEMacrodynamics, lfeMacrodynamics, lfeMacrodynamicsGrade);
                    ReportGrade(builder, language.LFEMicrodynamics, lfeMicrodynamics, lfeMicrodynamicsGrade);
                    builder.Append(language.ChestSlam).Append(valueIs).Append(language.Grades[Math.Max(lfePeakGrade, lfeMicrodynamicsGrade)])
                        .AppendLine();
                } else {
                    builder.AppendLine(language.NoLFE);
                }
                builder.AppendLine();

                ReportGrade(builder, language.SurroundUsage, surroundUsage, surroundGrade);
                if (statsEx.RelativeHeightLevel != 0) {
                    ReportGrade(builder, language.HeightUsage, heightUsage, heightGrade);
                }
            }

            Report = builder.ToString();
        }

        /// <summary>
        /// Print a graded metric.
        /// </summary>
        void ReportGrade(StringBuilder builder, string metric, float decibelValue, int grade) =>
            builder.Append(metric).Append(valueIs).Append(decibelValue.ToString("0.00 dB (")).Append(language.Grades[grade]).AppendLine(")");

        /// <summary>
        /// Globally cached metric/value separator, saves a few bytes of memory.
        /// </summary>
        const string valueIs = ": ";
    }
}
