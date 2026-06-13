using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

using Cavernize.Logic.Language;
using CavernizeGUI.Language;

namespace CavernizeGUI.Consts;

/// <summary>
/// Handle fetching of language strings and translations.
/// </summary>
sealed class Language {
    const string DefaultLanguage = "en-US";

    public string Code { get; }

    public TrackStrings TrackStrings { get; }

    public ConversionStrings ConversionStrings { get; }

    public ExternalConverterStrings ExternalConverterStrings { get; }

    public RenderReportStrings RenderReportStrings { get; }

    readonly IReadOnlyDictionary<string, string> mainWindowStrings;
    readonly IReadOnlyDictionary<string, string> renderTargetSelectorStrings;

    Language(string code, IReadOnlyDictionary<string, string> mainWindowStrings,
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

    public static Language Create(string languageCode) {
        string code = ResolveLanguage(languageCode);
        string resourceCode = code;
        IReadOnlyDictionary<string, string> mainWindow = LoadDictionary("MainWindowStrings", resourceCode);
        if (mainWindow.Count == 0) {
            resourceCode = DefaultLanguage;
            mainWindow = LoadDictionary("MainWindowStrings", resourceCode);
        }

        return new Language(code, mainWindow, LoadDictionary("RenderTargetSelectorStrings", resourceCode),
            LoadDictionary("TrackStrings", resourceCode),
            LoadDictionary("ConversionStrings", resourceCode), LoadDictionary("ExternalConverterStrings", resourceCode),
            LoadDictionary("RenderReportStrings", resourceCode));
    }

    public string this[string key] => mainWindowStrings[key];

    public string Text(string key) => mainWindowStrings[key];

    public string RenderTargetSelectorText(string key) => renderTargetSelectorStrings[key];

    public string MenuText(string key) => Text(key).Replace("_", string.Empty);

    public string FileTypeName(string key) {
        string value = Text(key);
        int separator = value.IndexOf('|');
        return separator < 0 ? value : value[..separator];
    }

    static string ResolveLanguage(string languageCode) {
        if (string.IsNullOrWhiteSpace(languageCode)) {
            languageCode = CultureInfo.CurrentUICulture.Name;
        } else if (languageCode == DefaultLanguage) {
            return DefaultLanguage;
        }

        return languageCode;
    }

    static IReadOnlyDictionary<string, string> LoadDictionary(string resource, string languageCode) {
        Assembly assembly = typeof(Language).Assembly;
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
        Dictionary<string, string> result = new Dictionary<string, string>();
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
        } catch (CultureNotFoundException) {
            return null;
        } catch (FileNotFoundException) {
            return null;
        }
    }

    static string NormalizeValue(string value) => string.Join("\n",
        value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Select(line => line.Trim())).Trim();
}
