using Cavern.CavernSettings;
using Cavern.Format.Common;

using Cavernize.Logic.CavernSettings;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;
using CavernizeGUI.CavernSettings;

namespace CavernizeGUI;

// Implementation of the main Cavernize interface
public partial class MainWindow : ICavernizeApp {
    /// <inheritdoc/>
    public bool Rendering => rendering;

    /// <inheritdoc/>
    public AudioFile LoadedFile { get; private set; }

    /// <inheritdoc/>
    public ExportFormat ExportFormat {
        get => SelectedExportFormat;
        set => SelectedExportFormat = value;
    }

    /// <inheritdoc/>
    public RenderTarget RenderTarget {
        get => SelectedRenderTarget;
        set => SelectedRenderTarget = value;
    }

    /// <inheritdoc/>
    public UpmixingSettings UpmixingSettings { get; }

    /// <inheritdoc/>
    public RenderingSettings RenderingSettings { get; }

    /// <inheritdoc/>
    public bool SurroundSwap {
        get => settings.SurroundSwap;
        set {
            settings.SurroundSwap = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    /// <inheritdoc/>
    public void OpenContent(string path) {
        Reset();
        ffmpeg.CheckFFmpeg();
        UpdateProgress(0);

        try {
            OpenContent(new AudioFile(path, language.TrackStrings));
        } catch (IOException) {
            Reset();
            throw;
        } catch (Exception e) {
            Reset();
            throw new AggregateException($"{e.Message} {language.TrackStrings.Later}", e);
        }
        settings.LastDirectory = Path.GetDirectoryName(path);
    }

    /// <inheritdoc/>
    public void OpenContent(AudioFile file) {
        LoadedFile = file;
        ApplyLoadedFile(file.Path);
    }

    /// <inheritdoc/>
    public Action GetRenderTask(string path) {
        try {
            PreRender();
        } catch (Exception e) {
            Error(e.Message);
            return null;
        }

        CavernizeTrack target = SelectedTrack;
        if (!ReportMode) {
            if (path == null) {
                return null;
            }

            try {
                return Render(path);
            } catch (Exception e) {
                Error(e.Message);
                return null;
            }
        } else {
            SetBlockSize(RenderTarget);
            try {
                return () => RenderTask(target, null, null);
            } catch (Exception e) {
                Error(e.Message);
                return null;
            }
        }
    }

    /// <inheritdoc/>
    public void RenderContent(string path) {
        Action renderTask = GetRenderTask(path);
        renderTask?.Invoke();
    }

    /// <summary>
    /// Clear the inner state of the application, resetting to when no content was loaded..
    /// </summary>
    public void Reset() {
        environment.Reset();
        if (LoadedFile != null && QueueJobs.FirstOrDefault(job => job.SourcePath == LoadedFile.Path) == null) {
            LoadedFile.Dispose();
            LoadedFile = null;
        }
        LoadedPath = null;
        LoadedTitle = Text("NoSrc");
        Tracks.Clear();
        SelectedTrack = null;
        report.Reset();
        ReportText = report.Report;
        Progress = 0;
        IsProgressIndeterminate = false;
    }
}
