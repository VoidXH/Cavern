using System;
using System.IO;
using System.Linq;
using System.Windows;

using Cavernize.Logic.CavernSettings;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using CavernizeGUI.CavernSettings;
using CavernizeGUI.Resources;

namespace CavernizeGUI;

// Implementation of the main Cavernize interface
partial class MainWindow : ICavernizeApp {
    /// <inheritdoc/>
    public bool Rendering => taskEngine.IsOperationRunning;

    /// <inheritdoc/>
    public AudioFile LoadedFile { get; private set; }

    /// <inheritdoc/>
    public ExportFormat ExportFormat {
        get => (ExportFormat)audio.SelectedItem;
        set => audio.SelectedItem = value;
    }

    /// <inheritdoc/>
    public RenderTarget RenderTarget {
        get => (RenderTarget)renderTarget.SelectedItem;
        set => renderTarget.SelectedItem = value;
    }

    /// <inheritdoc/>
    public CavernizeTrack SelectedTrack {
        get => trackInfo.SelectedTrack;
        set => trackInfo.SelectedTrack = value;
    }

    /// <inheritdoc/>
    public Cavern.CavernSettings.UpmixingSettings UpmixingSettings { get; } = new DynamicUpmixingSettings();

    /// <inheritdoc/>
    public RenderingSettings RenderingSettings { get; }

    /// <inheritdoc/>
    public bool SurroundSwap {
        get => Settings.Default.surroundSwap;
        set {
            Settings.Default.surroundSwap = value;
            Dispatcher.Invoke(() => surroundSwap.IsChecked = value);
        }
    }

    /// <inheritdoc/>
    public void OpenContent(string path) {
        Reset();
        ffmpeg.CheckFFmpeg();
        taskEngine.Progress = 0;
        OnOutputSelected(null, null);

        try {
            OpenContent(new AudioFile(path, Consts.Language.GetTrackStrings()));
        } catch (IOException) {
            Reset();
            throw;
        } catch (Exception e) {
            Reset();
            throw new AggregateException($"{e.Message} {Consts.Language.GetTrackStrings().Later}", e);
        }
        Settings.Default.lastDirectory = Path.GetDirectoryName(path);
    }

    /// <inheritdoc/>
    public void OpenContent(AudioFile file) {
        fileName.Text = Path.GetFileName(file.Path);
        LoadedFile = file;
        if (file.Tracks.Count != 0) {
            trackControls.Visibility = Visibility.Visible;
            tracks.ItemsSource = file.Tracks;
            CavernizeTrack bestQuality = file.Tracks
                .OrderBy(x => x.Codec)
                .FirstOrDefault();
            if (bestQuality != null) {
                tracks.SelectedItem = bestQuality;
            } else {
                tracks.SelectedIndex = 0;
            }
        }
    }

    /// <inheritdoc/>
    public Action GetRenderTask(string path) {
        try {
            PreRender();
        } catch (Exception e) {
            Error(e.Message);
            return null;
        }

        CavernizeTrack target = (CavernizeTrack)tracks.SelectedItem;
        if (!reportMode.IsChecked) {
            if (path == null) {
                path = AskUserForExportPath();
                if (path == null) {
                    return null;
                }
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
        Action renderTask;
        try {
            PreRender();
            renderTask = Render(path);
        } catch (Exception e) {
            Error(e.Message);
            return;
        }

        if (renderTask != null) {
            taskEngine.Run(renderTask, Error);
        }
    }

    /// <summary>
    /// Clear the inner state of the application, resetting to when no content was loaded..
    /// </summary>
    public void Reset() {
        environment.Reset();
        if (LoadedFile != null && queue.Jobs.FirstOrDefault(x => x.IsUsingFile(LoadedFile)) == null) {
            LoadedFile.Dispose();
            LoadedFile = null;
        }
        fileName.Text = string.Empty;
        trackControls.Visibility = Visibility.Hidden;
        tracks.ItemsSource = null;
        trackInfo.Reset();
        report = new(environment.Listener, Consts.Language.GetRenderReportStrings());
    }
}
