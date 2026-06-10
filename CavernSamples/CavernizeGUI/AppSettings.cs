using Settings = CavernizeGUI.Resources.Settings;

namespace CavernizeGUI;

sealed class AppSettings {
    readonly Settings settings = Settings.Default;
    readonly Resources.UpmixingSettings upmixingSettings = Resources.UpmixingSettings.Default;

    double viewScale = 1;
    bool muteBed;
    bool muteGround;
    bool detailedGrading;

    public string LastDirectory {
        get => settings.lastDirectory;
        set => settings.lastDirectory = value;
    }

    public string LastFilterDirectory {
        get => settings.lastOutputFilters;
        set => settings.lastOutputFilters = value;
    }

    public string HrirPath {
        get => settings.hrirPath;
        set => settings.hrirPath = value;
    }

    public string RoomCorrectionPath {
        get => null;
        set { }
    }

    public string FFmpegPath {
        get => settings.ffmpegLocation;
        set => settings.ffmpegLocation = value;
    }

    public string LanguageCode {
        get => settings.language;
        set => settings.language = value;
    }

    public DateTime LastUpdateCheck {
        get => settings.lastUpdate;
        set => settings.lastUpdate = value;
    }

    public double ViewScale {
        get => viewScale;
        set => viewScale = value;
    }

    public int OutputCodecIndex {
        get => settings.outputCodec;
        set => settings.outputCodec = value;
    }

    public int RenderTargetIndex {
        get => settings.renderTarget;
        set => settings.renderTarget = value;
    }

    public bool SpeakerVirtualizer {
        get => settings.speakerVirtualizer;
        set => settings.speakerVirtualizer = value;
    }

    public bool Force24Bit {
        get => settings.force24Bit;
        set => settings.force24Bit = value;
    }

    public bool MuteBed {
        get => muteBed;
        set => muteBed = value;
    }

    public bool MuteGround {
        get => muteGround;
        set => muteGround = value;
    }

    public bool SurroundSwap {
        get => settings.surroundSwap;
        set => settings.surroundSwap = value;
    }

    public bool WavChannelSkip {
        get => settings.wavChannelSkip;
        set => settings.wavChannelSkip = value;
    }

    public bool DetailedGrading {
        get => detailedGrading;
        set => detailedGrading = value;
    }

    public bool CheckUpdates {
        get => settings.checkUpdates;
        set => settings.checkUpdates = value;
    }

    public bool MatrixUpmixing {
        get => upmixingSettings.MatrixUpmix;
        set => upmixingSettings.MatrixUpmix = value;
    }

    public bool CavernizeUpmixing {
        get => upmixingSettings.Cavernize;
        set => upmixingSettings.Cavernize = value;
    }

    public float? UpmixingEffect {
        get => upmixingSettings.Effect;
        set => upmixingSettings.Effect = value ?? .75f;
    }

    public float? UpmixingSmoothness {
        get => upmixingSettings.Smoothness;
        set => upmixingSettings.Smoothness = value ?? .8f;
    }

    public static AppSettings Load() => new();

    public void Save() {
        settings.Save();
        upmixingSettings.Save();
    }
}
