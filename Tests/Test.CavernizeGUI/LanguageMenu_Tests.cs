namespace Test.CavernizeGUI;

[TestClass]
public class LanguageMenu_Tests {
    [TestMethod]
    public void ChineseLanguageOptionIsAvailableWithoutTranslatedSwitchText() {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string xaml = File.ReadAllText(Path.Combine(root, "CavernSamples", "CavernizeGUI", "MainWindow.xaml"));
        string settings = File.ReadAllText(Path.Combine(root, "CavernSamples", "CavernizeGUI", "MainWindow.Settings.cs"));
        string defaultStrings = File.ReadAllText(Path.Combine(root, "CavernSamples", "CavernizeGUI", "Language", "MainWindowStrings.cs"));
        string chineseStrings = File.ReadAllText(Path.Combine(root, "CavernSamples", "CavernizeGUI", "Language", "MainWindowStringsZH.cs"));

        Assert.IsTrue(xaml.Contains("{StaticResource LanZh}"));
        Assert.IsTrue(xaml.Contains("Click=\"LanguageChinese\""));
        Assert.IsTrue(settings.Contains("LanguageChinese"));
        Assert.IsTrue(settings.Contains("SetLanguage(\"zh-CN\")"));
        Assert.IsTrue(defaultStrings.Contains("[\"LanZh\"]"));
        Assert.IsFalse(chineseStrings.Contains("Set(\"LanZh\""));
    }
}
