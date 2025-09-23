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
    /// Codec to save the rendered content in.
    /// </summary>
    ExportFormat ExportFormat { get; set; }

    /// <summary>
    /// Channel layout for rendering the content in.
    /// </summary>
    RenderTarget RenderTarget { get; set; }

    /// <summary>
    /// The voltage gain at which the content is rendered. Shall default to 1.
    /// </summary>
    float RenderGain { get; set; }

    /// <summary>
    /// Access or modify settings related to matrix upmixing.
    /// </summary>
    UpmixingSettings UpmixingSettings { get; }

    /// <summary>
    /// Access or modify settings related to special rendering modes.
    /// </summary>
    SpecialRenderModeSettings SpecialRenderModeSettings { get; }

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
    /// Set up rendering to a target file. Doesn't start rendering in all implementations, but prepares the application for it.
    /// </summary>
    void RenderContent(string path);
}
