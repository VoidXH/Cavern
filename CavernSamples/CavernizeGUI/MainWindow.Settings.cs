using Avalonia;
using Avalonia.Controls;
using Cavern.Format.Common;
using System.Diagnostics;

namespace CavernizeGUI;

partial class MainWindow {
    MenuSection[] menuSections;

    void BuildNativeMenu() {
        if (NativeMenu.GetMenu(this) != null) {
            return;
        }

        NativeMenu menu = new NativeMenu();
        foreach (MenuSection section in GetMenuSections()) {
            NativeMenu submenu = new NativeMenu();
            foreach (MenuEntry entry in section.Entries) {
                submenu.Add(entry.IsSeparator ? new NativeMenuItemSeparator() : CreateNativeMenuItem(entry));
            }
            menu.Add(new NativeMenuItem(MenuText(section.HeaderKey)) {
                Menu = submenu
            });
        }
        menu.NeedsUpdate += (_, _) => UpdateMenuState();
        NativeMenu.SetMenu(this, menu);
        UpdateMenuState();
    }

    void BuildWindowsMenu() {
        WindowsMenuBarHost.IsVisible = true;
        if (WindowsRenderingMenu.Items.Count != 0) {
            return;
        }

        MenuItem[] roots = [
            WindowsRenderingMenu,
            WindowsViewMenu,
            WindowsLanguageMenu,
            WindowsHelpMenu
        ];
        MenuSection[] sections = GetMenuSections();
        TextBlock[] rootHeaders = [
            WindowsRenderingMenuText,
            WindowsViewMenuText,
            WindowsLanguageMenuText,
            WindowsHelpMenuText
        ];
        for (int i = 0; i < sections.Length; i++) {
            rootHeaders[i].Text = MenuText(sections[i].HeaderKey);
            foreach (MenuEntry entry in sections[i].Entries) {
                roots[i].Items.Add(entry.IsSeparator ? new Separator() : CreateWindowsMenuItem(entry));
            }
        }
        UpdateMenuState();
    }

    MenuSection[] GetMenuSections() => menuSections ??= [
        new("MenuR", [
            MenuEntry.Command("Upmix", "UpmixT", window => window.OpenUpmixSetup(null, EventArgs.Empty)),
            MenuEntry.ToggleAction("LoadV", "LoadVT", viewModel => viewModel.HasHrir,
                window => window.ToggleHrir(null, EventArgs.Empty)),
            MenuEntry.Toggle("SpVir", "SpVirT",
                viewModel => viewModel.SpeakerVirtualizer, (viewModel, value) => viewModel.SpeakerVirtualizer = value),
            MenuEntry.ToggleAction("FiltH", "FiltT", viewModel => viewModel.HasRoomCorrection,
                window => window.ToggleFilters(null, EventArgs.Empty)),
            MenuEntry.Command("FFLoc", "FFDes", window => window.LocateFFmpeg(null, null)),
            MenuEntry.Separator(),
            MenuEntry.Toggle("MuBeH", "MuBeT",
                viewModel => viewModel.MuteBed, (viewModel, value) => viewModel.MuteBed = value),
            MenuEntry.Toggle("MuGrH", "MuGrT",
                viewModel => viewModel.MuteGround, (viewModel, value) => viewModel.MuteGround = value),
            MenuEntry.Separator(),
            MenuEntry.Toggle("For24", "For24T",
                viewModel => viewModel.Force24Bit, (viewModel, value) => viewModel.Force24Bit = value),
            MenuEntry.Toggle("SuSwa", "SuSwaT",
                viewModel => viewModel.SurroundSwap, (viewModel, value) => viewModel.SurroundSwap = value,
                viewModel => !viewModel.SelectedExportFormat.Codec.IsEnvironmental()),
            MenuEntry.Toggle("WavCh", "WavChT",
                viewModel => viewModel.WavChannelSkip, (viewModel, value) => viewModel.WavChannelSkip = value),
            MenuEntry.Separator(),
            MenuEntry.Command("SMetH", "SMetT", window => window.ShowMetadata(null, null)),
            MenuEntry.Toggle("ReMoH", "ReMoT",
                viewModel => viewModel.ReportMode, (viewModel, value) => viewModel.ReportMode = value),
            MenuEntry.Toggle("DeGrH", "DeGrT",
                viewModel => viewModel.DetailedGrading, (viewModel, value) => viewModel.DetailedGrading = value),
            MenuEntry.Command("PReSh", "PReShT", window => window.ShowPostRenderReport(null, null)),
        ]),
        new("MenuV", [
            MenuEntry.Radio("50%", viewModel => Math.Abs(viewModel.ViewScale - .5) < .001, window => window.SetZoom(.5)),
            MenuEntry.Radio("75%", viewModel => Math.Abs(viewModel.ViewScale - .75) < .001, window => window.SetZoom(.75)),
            MenuEntry.Radio("100%", viewModel => Math.Abs(viewModel.ViewScale - 1) < .001, window => window.SetZoom(1)),
            MenuEntry.Radio("125%", viewModel => Math.Abs(viewModel.ViewScale - 1.25) < .001, window => window.SetZoom(1.25)),
        ]),
        new("MenuL", [
            MenuEntry.RadioKey("LanEn", viewModel => viewModel.LanguageCode == "en-US",
                window => window.LanguageEnglish(null, EventArgs.Empty)),
            MenuEntry.RadioKey("LanHu", viewModel => viewModel.LanguageCode == "hu-HU",
                window => window.LanguageHungarian(null, EventArgs.Empty)),
            MenuEntry.RadioKey("LanZh", viewModel => viewModel.LanguageCode == "zh-CN",
                window => window.LanguageChinese(null, EventArgs.Empty)),
#if DEBUG
            MenuEntry.RadioKey("LanTe", viewModel => viewModel.LanguageCode == "te-ST",
                window => window.LanguageTest(null, EventArgs.Empty)),
#endif
        ]),
        new("MenuH", [
            MenuEntry.Toggle("ChkUp", "ChkTt",
                viewModel => viewModel.CheckUpdates, (viewModel, value) => viewModel.CheckUpdates = value),
            MenuEntry.Separator(),
            MenuEntry.Command("UsrGu", null, window => window.Guide(null, null)),
            MenuEntry.Command("About", null, window => window.ShowAbout(null, null)),
        ]),
    ];

    NativeMenuItem CreateNativeMenuItem(MenuEntry entry) {
        NativeMenuItem item = new NativeMenuItem(MenuEntryHeader(entry)) {
            ToolTip = MenuEntryToolTip(entry)
        };
        if (entry.ToggleType.HasValue) {
            item.ToggleType = entry.ToggleType.Value;
        }
        item.Click += (_, _) => InvokeMenuEntry(entry);
        entry.NativeItem = item;
        return item;
    }

    MenuItem CreateWindowsMenuItem(MenuEntry entry) {
        MenuItem item = new MenuItem {
            Header = MenuEntryHeader(entry)
        };
        string toolTip = MenuEntryToolTip(entry);
        if (!string.IsNullOrWhiteSpace(toolTip)) {
            ToolTip.SetTip(item, toolTip);
        }
        if (entry.ToggleType.HasValue) {
            item.ToggleType = entry.ToggleType.Value;
        }
        item.Click += (_, _) => InvokeMenuEntry(entry);
        entry.WindowsItem = item;
        return item;
    }

    void InvokeMenuEntry(MenuEntry entry) {
        if (entry.SetChecked != null) {
            entry.SetChecked(this, !entry.IsChecked(this));
        } else {
            entry.Invoke(this);
        }
        UpdateMenuState();
    }

    void SetZoom(double scale) {
        ViewScale = scale;
        ApplyViewScale();
    }

    /// <summary>
    /// Set application language to English.
    /// </summary>
    void LanguageEnglish(object _, EventArgs __) => SetLanguageAndRestart("en-US");

    /// <summary>
    /// Set application language to Hungarian.
    /// </summary>
    void LanguageHungarian(object _, EventArgs __) => SetLanguageAndRestart("hu-HU");

    /// <summary>
    /// Set application language to Chinese.
    /// </summary>
    void LanguageChinese(object _, EventArgs __) => SetLanguageAndRestart("zh-CN");

    /// <summary>
    /// Set application language to an invalid, untranslated one.
    /// </summary>
    void LanguageTest(object _, EventArgs __) => SetLanguageAndRestart("te-ST");

    void SetLanguageAndRestart(string code) {
        if (SetLanguage(code)) {
            Restart();
        }
    }

    string MenuEntryHeader(MenuEntry entry) => entry.HeaderText ?? MenuText(entry.HeaderKey);

    string MenuEntryToolTip(MenuEntry entry) => entry.ToolTipKey == null ? null : Text(entry.ToolTipKey);

    void UpdateMenuState() {
        foreach (MenuEntry entry in GetMenuSections().SelectMany(section => section.Entries).Where(entry => !entry.IsSeparator)) {
            bool enabled = entry.IsEnabled(this);
            if (entry.NativeItem != null) {
                entry.NativeItem.IsEnabled = enabled;
            }
            if (entry.WindowsItem != null) {
                entry.WindowsItem.IsEnabled = enabled;
            }
            if (entry.IsChecked == null) {
                continue;
            }

            bool isChecked = entry.IsChecked(this);
            if (entry.NativeItem != null) {
                entry.NativeItem.IsChecked = isChecked;
            }
            if (entry.WindowsItem != null) {
                entry.WindowsItem.IsChecked = isChecked;
            }
        }
    }

    void ApplyViewScale() {
        double scale = ViewScale;

        Resources["HeaderFontSize"] = Scaled(26, scale);
        Resources["LabelFontSize"] = Scaled(16, scale);
        Resources["BodyFontSize"] = Scaled(16, scale);
        Resources["ButtonFontSize"] = Scaled(16, scale);
        Resources["PrimaryButtonFontSize"] = Scaled(26, scale);
        Resources["StatusFontSize"] = Scaled(22, scale);

        Resources["SystemColumnWidth"] = new GridLength(Scaled(360, scale));
        Resources["QueueColumnWidth"] = Scaled(365, scale);

        Resources["OuterPadding"] = new Thickness(Scaled(15, scale));
        Resources["SystemCardPadding"] = new Thickness(Scaled(20, scale));
        Resources["ContentCardPadding"] = new Thickness(Scaled(15, scale));
        Resources["StatusPadding"] = new Thickness(Scaled(20, scale), Scaled(10, scale));
        Resources["ButtonPadding"] = new Thickness(Scaled(14, scale), Scaled(8, scale));
        Resources["ComboPadding"] = new Thickness(Scaled(8, scale), Scaled(2, scale));
        Resources["WindowsMenuPadding"] = new Thickness(Scaled(10, scale), Scaled(4, scale));
        Resources["ComboArrowMargin"] = new Thickness(Scaled(8, scale), 0, 0, 0);
        Resources["ComboArrowSize"] = Scaled(12, scale);
        Resources["QueueItemPadding"] = new Thickness(Scaled(4, scale), Scaled(6, scale));
        Resources["SpeakerLayoutMargin"] = new Thickness(0, Scaled(8, scale));
        Resources["CardCornerRadius"] = new CornerRadius(Scaled(20, scale));
        Resources["ButtonCornerRadius"] = new CornerRadius(Scaled(18, scale));
        Resources["InfoButtonCornerRadius"] = new CornerRadius(Scaled(14, scale));

        Resources["WorkspaceSpacing"] = Scaled(15, scale);
        Resources["SystemRowSpacing"] = Scaled(12, scale);
        Resources["ContentRowSpacing"] = Scaled(14, scale);
        Resources["FormColumnSpacing"] = Scaled(8, scale);
        Resources["FormRowSpacing"] = Scaled(10, scale);
        Resources["ContentColumnSpacing"] = Scaled(10, scale);
        Resources["IconButtonSpacing"] = Scaled(9, scale);
        Resources["MenuHeaderSpacing"] = Scaled(8, scale);
        Resources["TrackDetailRowSpacing"] = Scaled(9, scale);
        Resources["QueueItemSpacing"] = Scaled(4, scale);

        Resources["InfoButtonSize"] = Scaled(28, scale);
        Resources["CompactRowHeight"] = Math.Max(28, Scaled(36, scale));
        Resources["ComboHeight"] = Math.Max(24, Scaled(31, scale));
        Resources["RenderTargetDropDownHeight"] = Scaled(300, scale);
        Resources["DropDownHeight"] = Scaled(260, scale);
        Resources["SecondaryButtonWidth"] = Scaled(210, scale);
        Resources["SecondaryButtonHeight"] = Scaled(45, scale);
        Resources["LogoWidth"] = Scaled(250, scale);
        Resources["LogoHeight"] = Scaled(56, scale);
        Resources["OpenButtonWidth"] = Scaled(165, scale);
        Resources["OpenButtonHeight"] = Scaled(45, scale);
        Resources["ActionButtonWidth"] = Scaled(225, scale);
        Resources["ActionButtonHeight"] = Scaled(52, scale);
        Resources["PrimaryButtonHeight"] = Scaled(60, scale);
        Resources["SmallIconSize"] = Scaled(24, scale);
        Resources["RenderIconSize"] = Scaled(34, scale);
        Resources["MenuIconSize"] = Scaled(28, scale);
        Resources["QueueButtonMinWidth"] = Scaled(155, scale);
        Resources["QueueRunButtonMinWidth"] = Scaled(130, scale);
        Resources["QueueProgressHeight"] = Scaled(8, scale);
        Resources["StatusProgressWidth"] = Scaled(225, scale);
        Resources["StatusProgressHeight"] = Scaled(30, scale);
    }

    static double Scaled(double value, double scale) => Math.Round(value * scale, 2);

    /// <summary>
    /// Relaunch the application when needed.
    /// </summary>
    void Restart() {
        if (Rendering) {
            Error(language["OpRes"]);
            return;
        }

        try {
            string processPath = Environment.ProcessPath;
            string appBundle = OperatingSystem.IsMacOS() ? GetAppBundle(processPath) : null;
            if (!string.IsNullOrWhiteSpace(appBundle)) {
                ProcessStartInfo startInfo = new ProcessStartInfo("open") {
                    UseShellExecute = false
                };
                startInfo.ArgumentList.Add("-n");
                startInfo.ArgumentList.Add(appBundle);
                Process.Start(startInfo);
            } else if (!string.IsNullOrWhiteSpace(processPath)) {
                Process.Start(new ProcessStartInfo(processPath) {
                    UseShellExecute = true
                });
            }
        } catch {
            // The language setting is already saved; the next manual launch will use it.
        }

        Close();
    }

    static string GetAppBundle(string processPath) {
        if (string.IsNullOrWhiteSpace(processPath)) {
            return null;
        }

        DirectoryInfo directory = new DirectoryInfo(Path.GetDirectoryName(processPath));
        while (directory != null) {
            if (directory.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase)) {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return null;
    }

    sealed class MenuSection(string headerKey, MenuEntry[] entries) {
        public string HeaderKey { get; } = headerKey;
        public MenuEntry[] Entries { get; } = entries;
    }

    sealed class MenuEntry {
        public string HeaderKey { get; init; }
        public string HeaderText { get; init; }
        public string ToolTipKey { get; init; }
        public MenuItemToggleType? ToggleType { get; init; }
        public Func<MainWindow, bool> IsChecked { get; init; }
        public Action<MainWindow, bool> SetChecked { get; init; }
        public Func<MainWindow, bool> IsEnabled { get; init; } = _ => true;
        public Action<MainWindow> Invoke { get; init; }
        public bool IsSeparator { get; init; }
        public NativeMenuItem NativeItem { get; set; }
        public MenuItem WindowsItem { get; set; }

        public static MenuEntry Command(string headerKey, string toolTipKey, Action<MainWindow> invoke) => new() {
            HeaderKey = headerKey,
            ToolTipKey = toolTipKey,
            Invoke = invoke
        };

        public static MenuEntry Toggle(string headerKey, string toolTipKey, Func<MainWindow, bool> isChecked,
            Action<MainWindow, bool> setChecked, Func<MainWindow, bool> isEnabled = null) => new() {
                HeaderKey = headerKey,
                ToolTipKey = toolTipKey,
                ToggleType = MenuItemToggleType.CheckBox,
                IsChecked = isChecked,
                SetChecked = setChecked,
                IsEnabled = isEnabled ?? (_ => true)
            };

        public static MenuEntry ToggleAction(string headerKey, string toolTipKey, Func<MainWindow, bool> isChecked,
            Action<MainWindow> invoke) => new() {
                HeaderKey = headerKey,
                ToolTipKey = toolTipKey,
                ToggleType = MenuItemToggleType.CheckBox,
                IsChecked = isChecked,
                Invoke = invoke
            };

        public static MenuEntry Radio(string headerText, Func<MainWindow, bool> isChecked, Action<MainWindow> invoke) =>
            new() {
                HeaderText = headerText,
                ToggleType = MenuItemToggleType.Radio,
                IsChecked = isChecked,
                Invoke = invoke
            };

        public static MenuEntry RadioKey(string headerKey, Func<MainWindow, bool> isChecked, Action<MainWindow> invoke) =>
            new() {
                HeaderKey = headerKey,
                ToggleType = MenuItemToggleType.Radio,
                IsChecked = isChecked,
                Invoke = invoke
            };

        public static MenuEntry Separator() => new() {
            IsSeparator = true
        };
    }
}
