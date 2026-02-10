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
    public AudioFile LoadedFile { get; private set; }

    /// <inheritdoc/>
    public CavernizeTrack SelectedTrack { get; set; }

    /// <inheritdoc/>
    public ExportFormat ExportFormat { get; set; } = new(Codec.PCM_LE, null, short.MaxValue, null);

    /// <inheritdoc/>
    public RenderTarget RenderTarget { get; set; } = new(null, ChannelPrototype.ref512);

    /// <inheritdoc/>
    public bool SurroundSwap { get; set; }

    /// <inheritdoc/>
    public UpmixingSettings UpmixingSettings => new(true);

    /// <inheritdoc/>
    public RenderingSettings RenderingSettings => new();

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

        LoadedFile = file;
        SelectedTrack = file.Tracks[0];
        environment.AttachToListener(SelectedTrack);

        string name = Path.GetFileNameWithoutExtension(file.Path);
        Console.WriteLine($"Opened {name} containing {SelectedTrack}.");
    }

    /// <inheritdoc/>
    public Action GetRenderTask(string path) {
        throw new NotImplementedException(); // TODO
    }

    /// <inheritdoc/>
    public void RenderContent(string path) => GetRenderTask(path).Invoke();

    /// <inheritdoc/>
    public void Reset() {
        throw new NotImplementedException(); // TODO
    }
}
