using Cavern.CavernSettings;

using Cavernize.Logic.CavernSettings;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;

namespace Test.Cavernize.Logic.Utilities;

/// <summary>
/// A non-rendering implementation of <see cref="ICavernizeApp"/> for testing purposes.
/// </summary>
class MockCavernizeApp : ICavernizeApp {
    /// <inheritdoc/>
    public bool Rendering { get; internal set; }

    /// <inheritdoc/>
    public AudioFile LoadedFile { get; private set; }

    /// <inheritdoc/>
    public CavernizeTrack SelectedTrack { get; set; }

    /// <inheritdoc/>
    public ExportFormat ExportFormat { get; set; }

    /// <inheritdoc/>
    public RenderTarget RenderTarget { get; set; }

    /// <inheritdoc/>
    public UpmixingSettings UpmixingSettings { get; internal set; }

    /// <inheritdoc/>
    public RenderingSettings RenderingSettings { get; internal set; }

    /// <inheritdoc/>
    public bool SurroundSwap { get; set; }

    /// <inheritdoc/>
    public void OpenContent(string path) => LoadedFile = new(path);

    /// <inheritdoc/>
    public void OpenContent(AudioFile file) => LoadedFile = file;

    /// <inheritdoc/>
    public Action GetRenderTask(string path) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void RenderContent(string path) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void Reset() => throw new NotImplementedException();
}
