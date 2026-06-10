using Cavern;
using Cavern.CavernSettings;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Utilities;
using Cavern.Virtualizer;

using Cavernize.Logic.CavernSettings;
using Cavernize.Logic.External;
using Cavernize.Logic.Language;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;
using VoidX.WPF.FFmpeg;

namespace CavernizeGUI;

/// <summary>
/// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
/// </summary>
public sealed partial class CavernizeSession : ICavernizeApp, IDisposable {
    /// <inheritdoc/>
    public bool Rendering { get; private set; }

    /// <inheritdoc/>
    public AudioFile LoadedFile { get; private set; }

    /// <inheritdoc/>
    public CavernizeTrack SelectedTrack { get; set; }

    /// <inheritdoc/>
    public ExportFormat ExportFormat { get; set; }

    /// <inheritdoc/>
    public RenderTarget RenderTarget { get; set; }

    /// <inheritdoc/>
    public UpmixingSettings UpmixingSettings { get; }

    /// <inheritdoc/>
    public RenderingSettings RenderingSettings { get; }

    /// <inheritdoc/>
    public bool SurroundSwap { get; set; }

    /// <summary>
    /// Skip writing WAVEFORMATEXTENSIBLE channel masks for WAV output.
    /// </summary>
    public bool WavChannelSkip { get; set; }

    /// <summary>
    /// Generate detailed render grading statistics.
    /// </summary>
    public bool DetailedGrading { get; set; }

    /// <summary>
    /// Render without writing an output file, only creating the post-render report.
    /// </summary>
    public bool ReportMode { get; set; }

    /// <summary>
    /// FFmpeg runner and locator.
    /// </summary>
    public FFmpeg FFmpeg { get; }

    /// <summary>
    /// Licence prompt used by external converters.
    /// </summary>
    public ILicence LicencePrompt { get; set; } = new RejectingLicence();

    /// <summary>
    /// Last generated render report.
    /// </summary>
    public PostRenderReport Report { get; private set; }

    /// <summary>
    /// Raised when render progress changes. Progress can be -1 when it is indeterminate.
    /// </summary>
    public event Action<double> ProgressChanged;

    /// <summary>
    /// Raised when render status text changes.
    /// </summary>
    public event Action<string> StatusChanged;

    /// <summary>
    /// Raised for non-fatal warnings.
    /// </summary>
    public event Action<string> WarningRaised;

    readonly TrackStrings trackStrings;
    readonly RenderReportStrings reportStrings;
    readonly ExternalConverterStrings externalConverterStrings;
    readonly AvaloniaLanguage language;
    readonly ConversionEnvironment environment;
    CancellationToken cancellationToken;
    int blockSize;

    /// <summary>
    /// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
    /// </summary>
    public CavernizeSession() : this(null, null, null, null, null) { }

    /// <summary>
    /// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
    /// </summary>
    public CavernizeSession(TrackStrings trackStrings, RenderReportStrings reportStrings,
        ExternalConverterStrings externalConverterStrings) : this(null, trackStrings, reportStrings, externalConverterStrings, null) { }

    /// <summary>
    /// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
    /// </summary>
    public CavernizeSession(FFmpeg ffmpeg) : this(ffmpeg, null, null, null, null) { }

    /// <summary>
    /// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
    /// </summary>
    internal CavernizeSession(AvaloniaLanguage language) :
        this(null, language.TrackStrings, language.RenderReportStrings, language.ExternalConverterStrings, language) { }

    /// <summary>
    /// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
    /// </summary>
    public CavernizeSession(FFmpeg ffmpeg, TrackStrings trackStrings, RenderReportStrings reportStrings,
        ExternalConverterStrings externalConverterStrings) : this(ffmpeg, trackStrings, reportStrings, externalConverterStrings, null) { }

    CavernizeSession(FFmpeg ffmpeg, TrackStrings trackStrings, RenderReportStrings reportStrings,
        ExternalConverterStrings externalConverterStrings, AvaloniaLanguage language) {
        this.language = language ?? AvaloniaLanguage.Create(null);
        this.trackStrings = trackStrings ?? this.language.TrackStrings;
        this.reportStrings = reportStrings ?? this.language.RenderReportStrings;
        this.externalConverterStrings = externalConverterStrings ?? this.language.ExternalConverterStrings;

        ExportFormat = ExportFormat.GetFormats(this.trackStrings).First(format => format.Codec == Codec.PCM_LE);
        RenderTarget = RenderTarget.Targets.FirstOrDefault(target => target.Name == "5.1.2 side") ?? RenderTarget.Targets[0];
        UpmixingSettings = new UpmixingSettings(true);
        RenderingSettings = new RenderingSettings();
        FFmpeg = ffmpeg ?? StatusFFmpeg.Create(UpdateStatus);

        environment = new ConversionEnvironment(this);
        Report = new PostRenderReport(environment.Listener, this.reportStrings);
    }

    /// <inheritdoc/>
    public void OpenContent(string path) {
        Reset();
        FFmpeg.CheckFFmpeg();
        UpdateProgress(0);

        try {
            OpenContent(new AudioFile(path, trackStrings));
        } catch (Exception) {
            Reset();
            throw;
        }
    }

    /// <inheritdoc/>
    public void OpenContent(AudioFile file) {
        LoadedFile = file ?? throw new ArgumentNullException(nameof(file));
        if (file.Tracks.Count == 0) {
            throw new TrackException("No supported audio tracks were found in the file.");
        }

        SelectedTrack = file.Tracks
            .Where(track => track.Codec != Codec.Unknown)
            .OrderBy(track => track.Codec)
            .FirstOrDefault() ?? file.Tracks[0];
    }

    /// <summary>
    /// Load a custom HRIR package for headphone virtualization.
    /// </summary>
    public void LoadHrir(string path) {
        using RIFFWaveReader file = new(path);
        file.ReadHeader();
        VirtualizerFilter.Override(
            VirtualChannel.Parse(new MultichannelWaveform(file.ReadMultichannelAfterHeader()), file.SampleRate),
            file.SampleRate);
    }

    /// <summary>
    /// Restore the built-in HRIR package for headphone virtualization.
    /// </summary>
    public void ResetHrir() => VirtualizerFilter.Reset();

    /// <inheritdoc/>
    public void Reset() {
        environment.Reset();
        LoadedFile?.Dispose();
        LoadedFile = null;
        SelectedTrack = null;
        Report = new PostRenderReport(environment.Listener, reportStrings);
        UpdateProgress(0);
    }

    /// <inheritdoc/>
    public void Dispose() => LoadedFile?.Dispose();

    void UpdateStatus(string text) => StatusChanged?.Invoke(text);

    void UpdateProgress(double progress) => ProgressChanged?.Invoke(progress);

    void ThrowIfCancellationRequested() => cancellationToken.ThrowIfCancellationRequested();

    const string waveExtension = ".wav";
    const int defaultWriteCacheLength = 16384;
    const int secondFrame = 2 * 1536;
    const double progressSplit = .95;
    const long updateInterval = 50000;
}
