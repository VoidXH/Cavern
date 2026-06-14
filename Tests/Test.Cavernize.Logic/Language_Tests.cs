using System.Reflection;

using Cavernize.Logic.Language;
using VoidX.WPF.Language;

namespace Test.Cavernize.Logic;

[TestClass]
public class Language_Tests {
    [TestMethod]
    public void ChineseTranslationsAreRegistered() {
        AssertHasChineseTranslation(new ConversionStrings());
        AssertHasChineseTranslation(new ExternalConverterStrings());
        AssertHasChineseTranslation(new TrackStrings());
        AssertHasChineseTranslation(new RenderReportStrings());
    }

    static void AssertHasChineseTranslation<T>(LanguageBase<T> language) where T : LanguageBase<T>, new() {
        LanguageBase<T>[] translations = (LanguageBase<T>[])typeof(LanguageBase<T>)
            .GetMethod("GetTranslations", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(language, null);
        Assert.IsTrue(translations.Any(IsChinese));
    }

    static bool IsChinese<T>(LanguageBase<T> language) where T : LanguageBase<T>, new() =>
        (string)typeof(LanguageBase<T>)
            .GetProperty("CultureCode", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(language) == "zh-CN";
}
