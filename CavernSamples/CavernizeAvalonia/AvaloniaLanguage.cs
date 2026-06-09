using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

using Cavern.Format.Common;
using Cavernize.Logic.Language;

namespace CavernizeAvalonia;

sealed class AvaloniaLanguage {
    const string DefaultLanguage = "en-US";
    const string Hungarian = "hu-HU";

    public string Code { get; }

    public TrackStrings TrackStrings { get; }

    public ConversionStrings ConversionStrings { get; }

    public ExternalConverterStrings ExternalConverterStrings { get; }

    public RenderReportStrings RenderReportStrings { get; }

    readonly IReadOnlyDictionary<string, string> mainWindowStrings;

    AvaloniaLanguage(string code, IReadOnlyDictionary<string, string> mainWindowStrings,
        IReadOnlyDictionary<string, string> trackStrings, IReadOnlyDictionary<string, string> conversionStrings,
        IReadOnlyDictionary<string, string> externalConverterStrings, IReadOnlyDictionary<string, string> renderReportStrings) {
        Code = code;
        this.mainWindowStrings = mainWindowStrings;
        TrackStrings = trackStrings.Count == 0 ? new TrackStrings() : new ResourceTrackStrings(trackStrings);
        ConversionStrings = conversionStrings.Count == 0 ? new ConversionStrings() : new ResourceConversionStrings(conversionStrings);
        ExternalConverterStrings = externalConverterStrings.Count == 0 ?
            new ExternalConverterStrings() :
            new ResourceExternalConverterStrings(externalConverterStrings);
        RenderReportStrings = renderReportStrings.Count == 0 ?
            new RenderReportStrings() :
            new ResourceRenderReportStrings(renderReportStrings);
    }

    public static AvaloniaLanguage Create(string languageCode) {
        string code = NormalizeLanguage(languageCode);
        IReadOnlyDictionary<string, string> mainWindow = LoadDictionary("MainWindowStrings", code);
        if (mainWindow.Count == 0) {
            code = DefaultLanguage;
            mainWindow = LoadDictionary("MainWindowStrings", code);
        }

        return new AvaloniaLanguage(code, mainWindow, LoadDictionary("TrackStrings", code),
            LoadDictionary("ConversionStrings", code), LoadDictionary("ExternalConverterStrings", code),
            LoadDictionary("RenderReportStrings", code));
    }

    public string Text(string key, string fallback) =>
        mainWindowStrings.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    public string MenuText(string key, string fallback) => Text(key, fallback).Replace("_", string.Empty);

    public string FileTypeName(string key, string fallback) {
        string value = Text(key, fallback);
        int separator = value.IndexOf('|');
        return separator < 0 ? value : value[..separator];
    }

    static string NormalizeLanguage(string languageCode) {
        if (string.IsNullOrWhiteSpace(languageCode)) {
            languageCode = CultureInfo.CurrentUICulture.Name;
        }

        return string.Equals(languageCode, Hungarian, StringComparison.OrdinalIgnoreCase) ? Hungarian : DefaultLanguage;
    }

    static IReadOnlyDictionary<string, string> LoadDictionary(string resource, string languageCode) {
        Assembly assembly = typeof(AvaloniaLanguage).Assembly;
        Assembly resourceAssembly = languageCode == DefaultLanguage ? assembly : GetSatelliteAssembly(assembly, languageCode);
        string file = $"{resource}.xaml";
        string resourceName = resourceAssembly?.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith($".{file}", StringComparison.OrdinalIgnoreCase));
        if (resourceName == null) {
            return new Dictionary<string, string>();
        }

        using Stream stream = resourceAssembly.GetManifestResourceStream(resourceName);
        if (stream == null) {
            return new Dictionary<string, string>();
        }

        XName xKey = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");
        Dictionary<string, string> result = new();
        foreach (XElement element in XDocument.Load(stream).Descendants().Where(element => element.Name.LocalName == "String")) {
            string key = (string)element.Attribute(xKey);
            if (!string.IsNullOrWhiteSpace(key)) {
                result[key] = NormalizeValue(element.Value);
            }
        }
        return result;
    }

    static Assembly GetSatelliteAssembly(Assembly assembly, string languageCode) {
        try {
            return assembly.GetSatelliteAssembly(new CultureInfo(languageCode));
        } catch (FileNotFoundException) {
            return null;
        }
    }

    static string NormalizeValue(string value) => string.Join("\n",
        value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Select(line => line.Trim())).Trim();

    sealed class ResourceTrackStrings(IReadOnlyDictionary<string, string> source) : TrackStrings {
        public override string NotSupported => Get("NoSup", base.NotSupported);

        public override string TypeEAC3JOC => Get("E3JOC", base.TypeEAC3JOC);

        public override string ObjectBasedTrack => Get("ObTra", base.ObjectBasedTrack);

        public override string ChannelBasedTrack => Get("ChTra", base.ChannelBasedTrack);

        public override string SourceChannels => Get("SouCh", base.SourceChannels);

        public override string MatrixedBeds => Get("MatBe", base.MatrixedBeds);

        public override string MatrixedObjects => Get("MatOb", base.MatrixedObjects);

        public override string BedChannels => Get("SouBe", base.BedChannels);

        public override string DynamicObjects => Get("SouDy", base.DynamicObjects);

        public override string Channels => Get("Chans", base.Channels);

        public override string WithObjects => Get("WiObj", base.WithObjects);

        public override string InvalidTrack => Get("InvTr", base.InvalidTrack);

        public override string Later => Get("Later", base.Later);

        protected override IReadOnlyDictionary<Codec, string> GetCodecNames() {
            IReadOnlyDictionary<Codec, string> defaults = base.GetCodecNames();
            return new Dictionary<Codec, string> {
                { Codec.PCM_Float, Get("PCM_Float", defaults[Codec.PCM_Float]) },
                { Codec.PCM_LE, Get("PCM_LE", defaults[Codec.PCM_LE]) },
            };
        }

        protected override IReadOnlyDictionary<Codec, string> GetExportFormats() {
            IReadOnlyDictionary<Codec, string> defaults = base.GetExportFormats();
            return new Dictionary<Codec, string> {
                { Codec.AC3, Get("C_AC3", defaults[Codec.AC3]) },
                { Codec.EnhancedAC3, Get("CEAC3", defaults[Codec.EnhancedAC3]) },
                { Codec.Opus, Get("COpus", defaults[Codec.Opus]) },
                { Codec.FLAC, Get("CFLAC", defaults[Codec.FLAC]) },
                { Codec.PCM_Float, Get("CPCMF", defaults[Codec.PCM_Float]) },
                { Codec.PCM_LE, Get("CPCMI", defaults[Codec.PCM_LE]) },
                { Codec.ADM_BWF, Get("CADMC", defaults[Codec.ADM_BWF]) },
                { Codec.ADM_BWF_Atmos, Get("CADMA", defaults[Codec.ADM_BWF_Atmos]) },
                { Codec.DAMF, Get("CDAMF", defaults[Codec.DAMF]) },
                { Codec.LimitlessAudio, Get("C_LAF", defaults[Codec.LimitlessAudio]) },
            };
        }

        string Get(string key, string fallback) =>
            source.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    sealed class ResourceConversionStrings(IReadOnlyDictionary<string, string> source) : ConversionStrings {
        public override string InvalidRootFile => Get("InvRo", base.InvalidRootFile);

        public override string ChannelFilterNotFound => Get("FiltN", base.ChannelFilterNotFound);

        string Get(string key, string fallback) =>
            source.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    sealed class ResourceExternalConverterStrings(IReadOnlyDictionary<string, string> source) : ExternalConverterStrings {
        public override string LicenceNeeded => Get("LicNe", base.LicenceNeeded);

        public override string LicenceFetch => Get("LicFe", base.LicenceFetch);

        public override string LicenceFail => Get("LicFa", base.LicenceFail);

        public override string WaitingUserAccept => Get("LicWa", base.WaitingUserAccept);

        public override string UserCancelled => Get("LicCa", base.UserCancelled);

        public override string Downloading => Get("ExDow", base.Downloading);

        public override string Extracting => Get("ExExt", base.Extracting);

        public override string ExtractingBitstream => Get("ExRaw", base.ExtractingBitstream);

        public override string Converting => Get("ConvW", base.Converting);

        public override string NetworkError => Get("DlErr", base.NetworkError);

        string Get(string key, string fallback) =>
            source.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    sealed class ResourceRenderReportStrings(IReadOnlyDictionary<string, string> source) : RenderReportStrings {
        public override string Default => Get("Defau", base.Default);

        public override string ActualBeds => Get("ABeds", base.ActualBeds);

        public override string ActualObjects => Get("AObjs", base.ActualObjects);

        public override string FakeTargets => Get("FakeT", base.FakeTargets);

        public override string PeakGain => Get("PeaGa", base.PeakGain);

        public override string RMSGain => Get("RMSGa", base.RMSGain);

        public override string Macrodynamics => Get("MacDy", base.Macrodynamics);

        public override string Microdynamics => Get("MicDy", base.Microdynamics);

        public override string NoLFE => Get("NoLFE", base.NoLFE);

        public override string LFEPeak => Get("PeaLF", base.LFEPeak);

        public override string LFERMS => Get("RMSLF", base.LFERMS);

        public override string LFEMacrodynamics => Get("MacLF", base.LFEMacrodynamics);

        public override string LFEMicrodynamics => Get("MicLF", base.LFEMicrodynamics);

        public override string ChestSlam => Get("CheSl", base.ChestSlam);

        public override string SurroundUsage => Get("SurUs", base.SurroundUsage);

        public override string HeightUsage => Get("HeiUs", base.HeightUsage);

        protected override string[] GetGrades() {
            string[] defaults = base.GetGrades();
            return [
                Get("Grad0", defaults[0]),
                Get("Grad1", defaults[1]),
                Get("Grad2", defaults[2]),
                Get("Grad3", defaults[3]),
                Get("Grad4", defaults[4]),
                Get("Grad5", defaults[5])
            ];
        }

        string Get(string key, string fallback) =>
            source.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }
}
