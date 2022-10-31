using Cavern.Format.Common;
using System;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// A rendering job with all relevant settings.
    /// </summary>
    class QueuedJob {
        /// <summary>
        /// Source audio file.
        /// </summary>
        readonly AudioFile source;

        /// <summary>
        /// Track in the source file to convert.
        /// </summary>
        readonly Track track;

        /// <summary>
        /// Speaker layout to use for export.
        /// </summary>
        readonly RenderTarget target;

        /// <summary>
        /// Output codec information.
        /// </summary>
        readonly ExportFormat format;

        /// <summary>
        /// Conversion-performing task.
        /// </summary>
        Action converter;

        /// <summary>
        /// A rendering job with all relevant settings.
        /// </summary>
        public QueuedJob(AudioFile source, Track track, RenderTarget target, ExportFormat format, Action converter) {
            this.source = source;
            this.track = track;
            this.target = target;
            this.format = format;
            this.converter = converter;
        }

        /// <summary>
        /// Checks if the source file is the checked <paramref name="file"/>.
        /// </summary>
        public bool IsUsingFile(AudioFile file) => source == file;

        /// <summary>
        /// Sets up the rendering environment for this job.
        /// </summary>
        public void Prepare(MainWindow window) {
            window.SetFile(source);
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