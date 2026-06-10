using Cavern.CavernSettings;

using Cavernize.Logic.CavernSettings;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;

namespace CavernizeGUI;

// Implementation of the main Cavernize interface
public partial class MainWindow : ICavernizeApp {
    /// <inheritdoc/>
    public bool Rendering => ViewModel.Session.Rendering;

    /// <inheritdoc/>
    public AudioFile LoadedFile => ViewModel.Session.LoadedFile;

    /// <inheritdoc/>
    public ExportFormat ExportFormat {
        get => ViewModel.SelectedExportFormat;
        set => ViewModel.SelectedExportFormat = value;
    }

    /// <inheritdoc/>
    public RenderTarget RenderTarget {
        get => ViewModel.SelectedRenderTarget;
        set => ViewModel.SelectedRenderTarget = value;
    }

    /// <inheritdoc/>
    public CavernizeTrack SelectedTrack {
        get => ViewModel.SelectedTrack;
        set => ViewModel.SelectedTrack = value;
    }

    /// <inheritdoc/>
    public UpmixingSettings UpmixingSettings => ViewModel.Session.UpmixingSettings;

    /// <inheritdoc/>
    public RenderingSettings RenderingSettings => ViewModel.Session.RenderingSettings;

    /// <inheritdoc/>
    public bool SurroundSwap {
        get => ViewModel.SurroundSwap;
        set => ViewModel.SurroundSwap = value;
    }

    /// <inheritdoc/>
    public void OpenContent(string path) {
        ViewModel.Session.OpenContent(path);
        ViewModel.SyncFromSession();
    }

    /// <inheritdoc/>
    public void OpenContent(AudioFile file) {
        ViewModel.Session.OpenContent(file);
        ViewModel.SyncFromSession();
    }

    /// <inheritdoc/>
    public Action GetRenderTask(string path) => ViewModel.Session.GetRenderTask(path);

    /// <inheritdoc/>
    public void RenderContent(string path) => ViewModel.Session.RenderContent(path);

    /// <inheritdoc/>
    public void Reset() {
        ViewModel.Session.Reset();
        ViewModel.SyncFromSession();
    }
}
