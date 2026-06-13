using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CavernizeGUI;

public sealed class QueuedRenderJob : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public string SourcePath { get; }

    public string OutputPath {
        get => outputPath;
        set {
            if (SetProperty(ref outputPath, value)) {
                OnPropertyChanged(nameof(OutputName));
            }
        }
    }

    public int? TrackIndex { get; }

    public ExportFormat ExportFormat { get; }

    public RenderTarget RenderTarget { get; }

    public bool SpeakerVirtualizer { get; }

    public bool Force24Bit { get; }

    public bool MuteBed { get; }

    public bool MuteGround { get; }

    public bool SurroundSwap { get; }

    public bool WavChannelSkip { get; }

    public bool DetailedGrading { get; }

    public bool MatrixUpmixing { get; }

    public bool CavernizeUpmixing { get; }

    public float UpmixingEffect { get; }

    public float UpmixingSmoothness { get; }

    public string DisplayName => Path.GetFileName(SourcePath);

    public string OutputName => Path.GetFileName(OutputPath);

    public string SettingsSummary => $"{RenderTarget.Name} {ExportFormat.Codec}";

    public double Progress {
        get => progress;
        set => SetProperty(ref progress, value);
    }

    public string Status {
        get => status;
        set => SetProperty(ref status, value);
    }

    string outputPath;
    double progress;
    string status = "Queued";

    public QueuedRenderJob(string sourcePath, string outputPath, int? trackIndex, ExportFormat exportFormat, RenderTarget renderTarget,
        bool speakerVirtualizer, bool force24Bit, bool muteBed, bool muteGround, bool surroundSwap, bool wavChannelSkip,
        bool detailedGrading, bool matrixUpmixing, bool cavernizeUpmixing, float upmixingEffect, float upmixingSmoothness) {
        SourcePath = sourcePath;
        OutputPath = outputPath;
        TrackIndex = trackIndex;
        ExportFormat = exportFormat;
        RenderTarget = renderTarget;
        SpeakerVirtualizer = speakerVirtualizer;
        Force24Bit = force24Bit;
        MuteBed = muteBed;
        MuteGround = muteGround;
        SurroundSwap = surroundSwap;
        WavChannelSkip = wavChannelSkip;
        DetailedGrading = detailedGrading;
        MatrixUpmixing = matrixUpmixing;
        CavernizeUpmixing = cavernizeUpmixing;
        UpmixingEffect = upmixingEffect;
        UpmixingSmoothness = upmixingSmoothness;
    }

    void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
