using Cavern.CavernSettings;

using Cavernize.Logic.CavernSettings;
using Cavernize.Logic.Models.RenderTargets;

namespace Cavernize.Logic.Models;

/// <summary>
/// Interface of a Cavernize converter implementation.
/// </summary>
public interface ICavernizeApp {
    /// <summary>
    /// A render is already in progress, further setting changes are not allowed.
    /// </summary>
    bool Rendering { get; }

    /// <summary>
    /// Currently loaded audio file or container of the content.
    /// </summary>
    AudioFile LoadedFile { get; }

    /// <summary>
    /// The track of the <see cref="LoadedFile"/> selected for rendering.
    /// </summary>
    CavernizeTrack SelectedTrack { get; set; }

    /// <summary>
    /// Codec to save the rendered content in.
    /// </summary>
    ExportFormat ExportFormat { get; set; }

    /// <summary>
    /// Channel layout for rendering the content in.
    /// </summary>
    RenderTarget RenderTarget { get; set; }

    /// <summary>
    /// Access or modify settings related to matrix upmixing.
    /// </summary>
    UpmixingSettings UpmixingSettings { get; }

    /// <summary>
    /// Access or modify settings related to special rendering modes.
    /// </summary>
    RenderingSettings RenderingSettings { get; }

    /// <summary>
    /// Swap the side and rear surround pair outputs.
    /// </summary>
    bool SurroundSwap { get; set; }

    /// <summary>
    /// Load a content file into the application for processing.
    /// </summary>
    void OpenContent(string path);

    /// <summary>
    /// Load an already opened <see cref="AudioFile"/> into the application for processing.
    /// </summary>
    void OpenContent(AudioFile file);

    /// <summary>
    /// Get the render task, than when run, exports the currently open content to the given <paramref name="path"/>.
    /// If the path is null, ask the user for an export path.
    /// </summary>
    /// <returns>A task for rendering or null when an error happened.</returns>
    /// <remarks>For direct rendering, use <see cref="RenderContent(string)"/>. This function exists for queue support.</remarks>
    Action GetRenderTask(string path);

    /// <summary>
    /// Start rendering the currently open content to the target output <paramref name="path"/>.
    /// This function immediately runs <see cref="GetRenderTask(string)"/> and handles errors.
    /// </summary>
    void RenderContent(string path);

    /// <summary>
    /// Clear the inner state of the application, resetting to when no content was loaded..
    /// </summary>
    void Reset();
}
