using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Cavern;
using Cavern.Channels;
using Cavern.Format.Renderers;
using Cavern.Utilities;

namespace CavernizeGUI {
    partial class MainWindow {
        /// <summary>
        /// Holds the post-render report.
        /// </summary>
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
        /// Print a graded metric.
        /// </summary>
        static void ReportGrade(StringBuilder builder, string metric, float decibelValue, int grade) =>
            builder.Append(metric).Append(valueIs).Append(decibelValue.ToString("0.00 dB ("))
                .Append(Consts.Language.GetRenderReportStrings()["Grad" + grade]).AppendLine(")");

        /// <summary>
        /// Print a raw signal level value in decibels, without grading.
        /// </summary>
        static void ReportValue(StringBuilder builder, string metric, float signalValue) =>
            builder.Append(metric).Append(valueIs).AppendLine(QMath.GainToDb(signalValue).ToString("0.00 dB"));

        /// <summary>
        /// Extracts the <see cref="report"/> from a render.
        /// </summary>
        void UpdatePostRenderReport(RenderStats stats) {
            int total = listener.ActiveSources.Count;
            int dynamic = total - stats.GetSemiStaticCount() - stats.GetSuperStaticCount();

            List<ReferenceChannel> channels = stats.GetStaticOrSemiStaticPositions()
                .Select(x => Renderer.ChannelFromPosition(x / Listener.EnvironmentSize)).ToList();
            channels.Sort();

            int unused = total - dynamic - channels.Count;
            if (unused < 0) {
                dynamic += unused;
                unused = 0;
            }

            StringBuilder builder = new();
            ResourceDictionary language = Consts.Language.GetRenderReportStrings();
            builder.Append(language["ABeds"]).Append(valueIs).Append(channels.Count);
            if (channels.Count != 0) {
                builder.Append(" (").Append(string.Join(", ", channels)).Append(')');
            }
            builder.AppendLine().Append(language["AObjs"]).Append(valueIs).AppendLine(dynamic.ToString())
                .Append(language["FakeT"]).Append(valueIs).AppendLine(unused.ToString());

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
                ReportValue(builder, (string)language["PeaGa"], statsEx.FrameLevelPeak);
                ReportValue(builder, (string)language["RMSGa"], statsEx.FrameLevelRMS);
                ReportGrade(builder, (string)language["MacDi"], macrodynamics, macrodynamicsGrade);
                ReportGrade(builder, (string)language["MicDi"], microdynamics, microdynamicsGrade);
                builder.AppendLine();

                ReportGrade(builder, (string)language["PeaLF"], lfePeak, lfePeakGrade);
                ReportValue(builder, (string)language["RMSLF"], statsEx.LFELevelRMS);
                ReportGrade(builder, (string)language["MacLF"], lfeMacrodynamics, lfeMacrodynamicsGrade);
                ReportGrade(builder, (string)language["MicLF"], lfeMicrodynamics, lfeMicrodynamicsGrade);
                builder.Append(language["CheSl"]).Append(valueIs).Append(language["Grad" + Math.Max(lfePeakGrade, lfeMicrodynamicsGrade)])
                    .AppendLine().AppendLine();

                ReportGrade(builder, (string)language["SurUs"], surroundUsage, surroundGrade);
                if (statsEx.RelativeHeightLevel != 0) {
                    ReportGrade(builder, (string)language["HeiUs"], heightUsage, heightGrade);
                }
            }

            report = builder.ToString();
        }

        /// <summary>
        /// Globally cached metric/value separator, saves a few bytes of memory.
        /// </summary>
        const string valueIs = ": ";
    }
}