using System;

using Cavern.Format.Common;

using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// A rendering job with all relevant settings.
    /// </summary>
    /// <param name="source">Source audio file</param>
    /// <param name="track">Track in the source file to convert</param>
    /// <param name="target">Speaker layout to use for export</param>
    /// <param name="format">Output codec information</param>
    /// <param name="converter">Conversion-performing task</param>
    class QueuedJob(AudioFile source, CavernizeTrack track, RenderTarget target, ExportFormat format, Action converter) {

        /// <summary>
        /// Checks if the source file is the checked <paramref name="file"/>.
        /// </summary>
        public bool IsUsingFile(AudioFile file) => source == file;

        /// <summary>
        /// Sets up the rendering environment for this job.
        /// </summary>
        public void Prepare(MainWindow window) {
            window.OpenContent(source);
            window.tracks.SelectedItem = track;
            window.renderTarget.SelectedItem = target;
            window.audio.SelectedItem = format;
        }

        /// <summary>
        /// Run this queued job, but only if it wasn't done already.
        /// </summary>
        public void Run() {
            if (converter != null) {
                converter();
                converter = null;
            }
        }

        /// <summary>
        /// Show this job's conversion plan.
        /// </summary>
        public override string ToString() {
            if (!format.Codec.IsEnvironmental()) {
                return $"{source} ({track.Codec} - {track.Language}) -> {target.Name} {format.Codec}";
            } else {
                return $"{source} ({track.Codec} - {track.Language}) -> {format.Codec}";
            }
        }
    }
}