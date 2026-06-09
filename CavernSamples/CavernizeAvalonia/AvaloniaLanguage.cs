using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

using Cavernize.Logic.Language;
using CavernizeAvalonia.Language;

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
    readonly IReadOnlyDictionary<string, string> renderTargetSelectorStrings;

    AvaloniaLanguage(string code, IReadOnlyDictionary<string, string> mainWindowStrings,
        IReadOnlyDictionary<string, string> renderTargetSelectorStrings,
        IReadOnlyDictionary<string, string> trackStrings, IReadOnlyDictionary<string, string> conversionStrings,
        IReadOnlyDictionary<string, string> externalConverterStrings, IReadOnlyDictionary<string, string> renderReportStrings) {
        Code = code;
        this.mainWindowStrings = mainWindowStrings;
        this.renderTargetSelectorStrings = renderTargetSelectorStrings;
        TrackStrings = trackStrings.Count == 0 ? new TrackStrings() : new DynamicTrackStrings(trackStrings);
        ConversionStrings = conversionStrings.Count == 0 ? new ConversionStrings() : new DynamicConversionStrings(conversionStrings);
        ExternalConverterStrings = externalConverterStrings.Count == 0 ?
            new ExternalConverterStrings() :
            new DynamicExternalConverterStrings(externalConverterStrings);
        RenderReportStrings = renderReportStrings.Count == 0 ?
            new RenderReportStrings() :
            new DynamicRenderReportStrings(renderReportStrings);
    }

    public static AvaloniaLanguage Create(string languageCode) {
        string code = NormalizeLanguage(languageCode);
        IReadOnlyDictionary<string, string> mainWindow = LoadDictionary("MainWindowStrings", code);
        if (mainWindow.Count == 0) {
            code = DefaultLanguage;
            mainWindow = LoadDictionary("MainWindowStrings", code);
        }

        return new AvaloniaLanguage(code, mainWindow, LoadDictionary("RenderTargetSelectorStrings", code),
            LoadDictionary("TrackStrings", code),
            LoadDictionary("ConversionStrings", code), LoadDictionary("ExternalConverterStrings", code),
            LoadDictionary("RenderReportStrings", code));
    }

    public string this[string key] => mainWindowStrings[key];

    public string Text(string key, string fallback) =>
        mainWindowStrings.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    public string RenderTargetSelectorText(string key, string fallback) =>
        renderTargetSelectorStrings.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

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
}
