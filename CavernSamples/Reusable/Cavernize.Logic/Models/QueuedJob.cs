using Cavern.Format.Common;

using Cavernize.Logic.Models.RenderTargets;

namespace Cavernize.Logic.Models;

/// <summary>
/// A rendering job with all relevant settings.
/// </summary>
public sealed class QueuedJob {
    /// <summary>
    /// Queued audio file.
    /// </summary>
    readonly AudioFile source;

    /// <summary>
    /// Track in the source file to convert.
    /// </summary>
    readonly CavernizeTrack track;

    /// <summary>
    /// Speaker layout to use for export.
    /// </summary>
    readonly RenderTarget target;

    /// <summary>
    /// >Render the <see cref="source"/> to this codec.
    /// </summary>
    readonly ExportFormat format;

    /// <summary>
    /// Task that performs the conversion.
    /// </summary>
    Action converter;

    /// <summary>
    /// A rendering job with all relevant settings.
    /// </summary>
    /// <param name="source">Queued audio file</param>
    /// <param name="track">Track in the source file to convert</param>
    /// <param name="target">Speaker layout to use for export</param>
    /// <param name="format">Render the <paramref name="source"/> to this codec</param>
    /// <param name="converter">Task that performs the conversion</param>
    public QueuedJob(AudioFile source, CavernizeTrack track, RenderTarget target, ExportFormat format, Action converter) {
        ArgumentNullException.ThrowIfNull(nameof(source));
        ArgumentNullException.ThrowIfNull(nameof(track));
        ArgumentNullException.ThrowIfNull(nameof(target));
        ArgumentNullException.ThrowIfNull(nameof(format));
        ArgumentNullException.ThrowIfNull(nameof(converter));

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
    public void Prepare(ICavernizeApp app) {
        app.OpenContent(source);
        app.SelectedTrack = track;
        app.RenderTarget = target;
        app.ExportFormat = format;
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
