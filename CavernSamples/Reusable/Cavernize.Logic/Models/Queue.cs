using System.Collections.ObjectModel;
using System.Collections.Specialized;

using VoidX.WPF.FFmpeg;

using Cavernize.Logic.Rendering;

namespace Cavernize.Logic.Models;

/// <summary>
/// Handles queueing and organizing/running the queue.
/// </summary>
/// <param name="app">Affected running instance of Cavernize</param>
public class Queue(ICavernizeApp app) {
    /// <summary>
    /// Queued conversions.
    /// </summary>
    public ObservableCollection<QueuedJob> Jobs { get; set; } = [];

    /// <summary>
    /// Add the currently set up processing to the queue with a custom output <paramref name="path"/>.
    /// If the <paramref name="path"/> is null, the user will be asked where to export it.
    /// </summary>
    public bool AddCurrent(string path) {
        Action renderTask = app.GetRenderTask(path);
        if (renderTask != null) {
            AddRenderTask(renderTask);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Queue one or more input <paramref name="files"/>, prompting the user to select a distinct output path for all of them.
    /// </summary>
    public void AddRange(StringCollection files, List<string> invalids) {
        int c = files.Count;
        for (int i = 0; i < c; i++) {
            try {
                app.OpenContent(files[i]);
            } catch {
                invalids.Add(Path.GetFileName(files[i]));
                continue;
            }

            QueueRenderTask(app.GetRenderTask(null), invalids, Path.GetFileName(files[i]));
        }
        if (c > 1) {
            app.Reset(); // All content shall be on the queue only on multiple added items
        }
    }

    /// <summary>
    /// Queue one or more input <paramref name="files"/> that should be rendered to a <paramref name="targetFolder"/>.
    /// The files that couldn't be processed will be put on the list of <paramref name="invalids"/>.
    /// </summary>
    public void AddRange(StringCollection files, string targetFolder, List<string> invalids, FFmpeg ffmpeg) {
        int c = files.Count;
        for (int i = 0; i < c; i++) {
            try {
                app.OpenContent(files[i]);
            } catch {
                invalids.Add(Path.GetFileName(files[i]));
                continue;
            }

            string container = MergeToContainer.GetPossibleContainers(app.SelectedTrack, app.ExportFormat.Codec, ffmpeg);
            container = container.Substring(container.IndexOf('|') + 2, 4);
            string outputPath = Path.Combine(targetFolder, Path.GetFileNameWithoutExtension(files[i])) + container;
            QueueRenderTask(app.GetRenderTask(outputPath), invalids, Path.GetFileName(files[i]));
        }
        if (c > 1) {
            app.Reset(); // All content shall be on the queue only on multiple added items
        }
    }

    /// <summary>
    /// Add the currently selected rendering configuration to the queue with a pre-calculated rendering process.
    /// </summary>
    void AddRenderTask(Action renderTask) => Jobs.Add(new QueuedJob(app.LoadedFile, app.SelectedTrack, app.RenderTarget, app.ExportFormat, renderTask));

    /// <summary>
    /// Add the currently loaded track to the queue, or add its <paramref name="fileName"/> to the list of
    /// <paramref name="invalids"/> if rendering is not possible for any reason.
    /// </summary>
    void QueueRenderTask(Action renderTask, List<string> invalids, string fileName) {
        if (renderTask != null) {
            AddRenderTask(renderTask);
        } else {
            invalids.Add(fileName);
        }
    }
}
