using System;
using System.IO;

using Cavern.CavernSettings;
using Cavern.Channels;
using Cavern.Format.Common;

using Cavernize.Logic.CavernSettings;
using Cavernize.Logic.CommandLine;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;

namespace CavernizeCLI;

/// <summary>
/// Cavernize in the command line to support all platforms that run .NET.
/// </summary>
public class Program : ICavernizeApp {
    /// <inheritdoc/>
    public bool Rendering { get; private set; }

    /// <inheritdoc/>
    public string FilePath => file?.Path;

    /// <inheritdoc/>
    public ExportFormat ExportFormat { get; set; } = new(Codec.PCM_LE, null, short.MaxValue, null);

    /// <inheritdoc/>
    public RenderTarget RenderTarget { get; set; } = new(null, ChannelPrototype.ref512);

    /// <inheritdoc/>
    public float RenderGain { get; set; } = 1;

    /// <inheritdoc/>
    public UpmixingSettings UpmixingSettings => new(true);

    /// <inheritdoc/>
    public SpecialRenderModeSettings SpecialRenderModeSettings => new();

    /// <summary>
    /// Last opened audio file.
    /// </summary>
    AudioFile file;

    /// <summary>
    /// Render process handler.
    /// </summary>
    ConversionEnvironment environment;

    /// <summary>
    /// Application entry point.
    /// </summary>
    public static void Main(string[] args) {
        Program app = new();
        app.environment = new(app);
        CommandLineProcessor.Initialize(args, app);
    }

    /// <inheritdoc/>
    public void OpenContent(string path) => OpenContent(new AudioFile(path, new()));

    /// <inheritdoc/>
    public void OpenContent(AudioFile file) {
        if (file.Tracks.Count == 0) {
            throw new CommandException("No supported audio tracks were found in the file.");
        }

        this.file = file;
        environment.AttachToListener(file.Tracks[0], false);

        string name = Path.GetFileNameWithoutExtension(file.Path);
        Console.WriteLine($"Opened {name} containing {file.Tracks[0]}.");
    }

    /// <inheritdoc/>
    public void RenderContent(string path) {
        throw new NotImplementedException(); // TODO
    }
}
