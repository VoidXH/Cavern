using System.Text.Json;

namespace CavernizeAvalonia;

sealed class AppSettings {
    public string LastDirectory { get; set; }

    public string LastFilterDirectory { get; set; }

    public string HrirPath { get; set; }

    public string RoomCorrectionPath { get; set; }

    public string FFmpegPath { get; set; }

    public string LanguageCode { get; set; }

    public DateTime LastUpdateCheck { get; set; }

    public double ViewScale { get; set; } = 1;

    public string ExportCodec { get; set; }

    public string RenderTarget { get; set; }

    public bool SpeakerVirtualizer { get; set; }

    public bool Force24Bit { get; set; }

    public bool MuteBed { get; set; }

    public bool MuteGround { get; set; }

    public bool SurroundSwap { get; set; }

    public bool WavChannelSkip { get; set; }

    public bool DetailedGrading { get; set; }

    public bool CheckUpdates { get; set; }

    public bool MatrixUpmixing { get; set; }

    public bool CavernizeUpmixing { get; set; }

    public float? UpmixingEffect { get; set; }

    public float? UpmixingSmoothness { get; set; }

    static readonly JsonSerializerOptions serializerOptions = new() {
        WriteIndented = true
    };

    public static AppSettings Load() {
        try {
            return File.Exists(SettingsPath) ?
                JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), serializerOptions) ?? new AppSettings() :
                new AppSettings();
        } catch {
            return new AppSettings();
        }
    }

    public void Save() {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, serializerOptions));
    }

    static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Cavernize",
        "settings.json");
}
