using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Cavern;
using Cavern.Format.Common;
using Cavern.Utilities;
using Cavernize.Logic.Models.RenderTargets;
using System.Diagnostics;
using System.Text;
using VoidX.WPF;

namespace CavernizeGUI;

public partial class MainWindow : Window {
    MenuSection[] menuSections;
    bool renderTargetSelectorOpen;

    public MainWindow() => InitializeComponent();

    MainViewModel ViewModel => (MainViewModel)DataContext;

    protected override void OnOpened(EventArgs e) {
        base.OnOpened(e);
        ApplyViewScale();
        BuildNativeMenu();
        BuildWindowsMenu();
        _ = CheckForUpdates();
    }

    protected override void OnClosed(EventArgs e) {
        ViewModel.Dispose();
        base.OnClosed(e);
    }

    void BuildNativeMenu() {
        if (NativeMenu.GetMenu(this) != null) {
            return;
        }

        NativeMenu menu = new();
        foreach (MenuSection section in GetMenuSections()) {
            NativeMenu submenu = new();
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
            MenuEntry.RadioKey("LanEn", viewModel => viewModel.LanguageCode == "en-US", window => window.SetLanguage("en-US")),
            MenuEntry.RadioKey("LanHu", viewModel => viewModel.LanguageCode == "hu-HU", window => window.SetLanguage("hu-HU")),
        ]),
        new("MenuH", [
            MenuEntry.Toggle("ChkUp", "ChkTt",
                viewModel => viewModel.CheckUpdates, (viewModel, value) => viewModel.CheckUpdates = value),
            MenuEntry.Separator(),
            MenuEntry.Command("UsrGu", null, window => window.OpenUserGuide(null, null)),
            MenuEntry.Command("About", null, window => window.ShowAbout(null, null)),
        ]),
    ];

    NativeMenuItem CreateNativeMenuItem(MenuEntry entry) {
        NativeMenuItem item = new(MenuEntryHeader(entry)) {
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
        MenuItem item = new() {
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
            MainViewModel viewModel = ViewModel;
            entry.SetChecked(viewModel, !entry.IsChecked(viewModel));
        } else {
            entry.Invoke(this);
        }
        UpdateMenuState();
    }

    void SetZoom(double scale) {
        ViewModel.ViewScale = scale;
        ApplyViewScale();
    }

    void SetLanguage(string code) {
        if (ViewModel.SetLanguage(code)) {
            Restart();
        }
    }

    string MenuEntryHeader(MenuEntry entry) => entry.HeaderText ?? MenuText(entry.HeaderKey);

    string MenuEntryToolTip(MenuEntry entry) => entry.ToolTipKey == null ? null : Text(entry.ToolTipKey);

    void UpdateMenuState() {
        MainViewModel viewModel = ViewModel;
        foreach (MenuEntry entry in GetMenuSections().SelectMany(section => section.Entries).Where(entry => !entry.IsSeparator)) {
            bool enabled = entry.IsEnabled(viewModel);
            if (entry.NativeItem != null) {
                entry.NativeItem.IsEnabled = enabled;
            }
            if (entry.WindowsItem != null) {
                entry.WindowsItem.IsEnabled = enabled;
            }
            if (entry.IsChecked == null) {
                continue;
            }

            bool isChecked = entry.IsChecked(viewModel);
            if (entry.NativeItem != null) {
                entry.NativeItem.IsChecked = isChecked;
            }
            if (entry.WindowsItem != null) {
                entry.WindowsItem.IsChecked = isChecked;
            }
        }
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
        public Func<MainViewModel, bool> IsChecked { get; init; }
        public Action<MainViewModel, bool> SetChecked { get; init; }
        public Func<MainViewModel, bool> IsEnabled { get; init; } = _ => true;
        public Action<MainWindow> Invoke { get; init; }
        public bool IsSeparator { get; init; }
        public NativeMenuItem NativeItem { get; set; }
        public MenuItem WindowsItem { get; set; }

        public static MenuEntry Command(string headerKey, string toolTipKey, Action<MainWindow> invoke) => new() {
            HeaderKey = headerKey,
            ToolTipKey = toolTipKey,
            Invoke = invoke
        };

        public static MenuEntry Toggle(string headerKey, string toolTipKey, Func<MainViewModel, bool> isChecked,
            Action<MainViewModel, bool> setChecked, Func<MainViewModel, bool> isEnabled = null) => new() {
                HeaderKey = headerKey,
                ToolTipKey = toolTipKey,
                ToggleType = MenuItemToggleType.CheckBox,
                IsChecked = isChecked,
                SetChecked = setChecked,
                IsEnabled = isEnabled ?? (_ => true)
            };

        public static MenuEntry ToggleAction(string headerKey, string toolTipKey, Func<MainViewModel, bool> isChecked,
            Action<MainWindow> invoke) => new() {
                HeaderKey = headerKey,
                ToolTipKey = toolTipKey,
                ToggleType = MenuItemToggleType.CheckBox,
                IsChecked = isChecked,
                Invoke = invoke
            };

        public static MenuEntry Radio(string headerText, Func<MainViewModel, bool> isChecked, Action<MainWindow> invoke) =>
            new() {
                HeaderText = headerText,
                ToggleType = MenuItemToggleType.Radio,
                IsChecked = isChecked,
                Invoke = invoke
            };

        public static MenuEntry RadioKey(string headerKey, Func<MainViewModel, bool> isChecked, Action<MainWindow> invoke) =>
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

    async void OpenUpmixSetup(object sender, EventArgs e) {
        MainViewModel viewModel = ViewModel;
        UpmixingSetupWindow dialog = new(viewModel);
        await dialog.ShowDialog(this);
        if (dialog.Accepted) {
            dialog.ApplyTo(viewModel);
            UpdateMenuState();
        }
    }

    async void OpenFile(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        MainViewModel viewModel = ViewModel;
        string[] paths = await PickFilePaths(new FilePickerOpenOptions {
            Title = viewModel.OpenSourcePickerTitle,
            AllowMultiple = true,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
            FileTypeFilter = [
                new FilePickerFileType(viewModel.AudioVideoFileType) {
                    Patterns = Cavern.Format.AudioReader.filter.Split(';')
                },
                FilePickerFileTypes.All
            ]
        });
        if (paths.Length == 1) {
            await viewModel.OpenFile(paths[0]);
        } else if (paths.Length > 1) {
            await AddFilesToQueue(paths);
        }
    }

    async void OpenRenderTargetSelector(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (renderTargetSelectorOpen) {
            return;
        }

        renderTargetSelectorOpen = true;
        try {
            RenderTarget selected = await new RenderTargetSelectorWindow(ViewModel).ShowDialog<RenderTarget>(this);
            if (selected != null) {
                ViewModel.SelectedRenderTarget = selected;
            }
        } finally {
            renderTargetSelectorOpen = false;
        }
    }

    async void RenderFile(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (StorageProvider == null) {
            return;
        }

        MainViewModel viewModel = ViewModel;
        string path = null;
        if (!viewModel.ReportMode) {
            IStorageFile file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
                Title = viewModel.SaveRenderPickerTitle,
                SuggestedFileName = viewModel.SuggestedOutputName,
                DefaultExtension = viewModel.SuggestedOutputExtension,
                SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
                FileTypeChoices = [
                    new FilePickerFileType(viewModel.SelectedFormatFileType) {
                        Patterns = [$"*.{viewModel.SuggestedOutputExtension}"]
                    },
                    FilePickerFileTypes.All
                ]
            });
            path = file?.Path.LocalPath;
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }
        }

        await viewModel.RenderTo(path);
    }

    void AddToQueue(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ViewModel.AddCurrentToQueue();
        ExpandForQueue(ViewModel);
    }

    async void RunQueue(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        await ViewModel.RunQueue();
    }

    void Cancel(object sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel.Cancel();

    void RemoveQueued(object sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel.RemoveSelectedQueueJob();

    void ShowWiring(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ShowTextWindow(ViewModel.DisplayWiringText, ViewModel.GetWiringText());
    }

    void ShowSystemInfo(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ShowTextWindow(ViewModel.SystemTitle, ViewModel.SystemInfoText);
    }

    async void LocateFFmpeg(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        MainViewModel viewModel = ViewModel;
        string path = await PickSingleFilePath(new FilePickerOpenOptions {
            Title = viewModel.Text("FFLoc", "Locate FFmpeg"),
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
            FileTypeFilter = [FilePickerFileTypes.All]
        });
        if (!string.IsNullOrWhiteSpace(path)) {
            viewModel.SetFfmpegLocation(path);
        }
    }

    void ShowMetadata(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        string title = ViewModel.Text("CMetT", "Codec metadata");
        if (ViewModel.SelectedTrack == null) {
            ShowTextWindow(title, ViewModel.Text("CMeET", "Please load a file first."));
            return;
        }

        ReadableMetadata metadata = ViewModel.SelectedTrack.GetMetadata();
        if (metadata == null) {
            ShowTextWindow(title, ViewModel.Text("CMeUT",
                "The Cavern API does not yet support displaying the metadata of the selected track."));
            return;
        }

        new MetadataWindow(metadata, title).Show(this);
    }

    void ShowPostRenderReport(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ShowTextWindow(ViewModel.Text("PReRe", "Post-render report"), ViewModel.GetPostRenderReportText());
    }

    void OpenUserGuide(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        OpenUrl("https://cavern.sbence.hu/cavern/doc.php?p=Cavernize", Text("UsrGu", "User guide"));
    }

    void OpenCavernWebsite(object sender, PointerPressedEventArgs e) => OpenUrl("https://cavern.sbence.hu", "Cavern");

    void OpenUrl(string url, string title) {
        try {
            Process.Start(new ProcessStartInfo(url) {
                UseShellExecute = true
            });
        } catch {
            ShowTextWindow(title, url);
        }
    }

    void ShowAbout(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        StringBuilder result = new(Listener.Info);
        if (CavernAmp.Available) {
            result.Append('\n').Append(ViewModel.Text("AbouA", "Performance accelerated with CavernAmp."));
        }

        result.AppendLine().Append("Build: ");
        FileInfo cavernizeLogic = new(Path.Combine(AppContext.BaseDirectory, "Cavernize.Logic.dll"));
        FileInfo cavernizeGui = new(Path.Combine(AppContext.BaseDirectory, "CavernizeGUI.dll"));
        result.Append(cavernizeLogic.Exists ? cavernizeLogic.CreationTime : "unknown").Append(", ")
            .Append(cavernizeGui.Exists ? cavernizeGui.CreationTime : "unknown");
        ShowTextWindow(ViewModel.Text("AbouH", "About"), result.ToString());
    }

    async void ToggleHrir(object sender, EventArgs e) {
        if (ViewModel.HasHrir) {
            ViewModel.ResetHrir();
        } else {
            await LoadHrir();
        }
        UpdateMenuState();
    }

    async void LoadHrir(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        await LoadHrir();
    }

    async Task LoadHrir() {
        MainViewModel viewModel = ViewModel;
        string path = await PickSingleFilePath(new FilePickerOpenOptions {
            Title = viewModel.LoadHrirTitle,
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
            FileTypeFilter = [
                new FilePickerFileType(viewModel.ImpulseResponseFileType) {
                    Patterns = ["*.wav"]
                },
                FilePickerFileTypes.All
            ]
        });
        if (!string.IsNullOrWhiteSpace(path)) {
            await viewModel.LoadHrir(path);
        }
        UpdateMenuState();
    }

    void ResetHrir(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ViewModel.ResetHrir();
        UpdateMenuState();
    }

    async void ToggleFilters(object sender, EventArgs e) {
        if (ViewModel.HasRoomCorrection) {
            ViewModel.ClearRoomCorrection();
        } else {
            await LoadFilters();
        }
        UpdateMenuState();
    }

    async void LoadFilters(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        await LoadFilters();
    }

    async Task LoadFilters() {
        MainViewModel viewModel = ViewModel;
        string path = await PickSingleFilePath(new FilePickerOpenOptions {
            Title = viewModel.LoadFiltersTitle,
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastFilterDirectory),
            FileTypeFilter = [
                new FilePickerFileType(viewModel.RoomCorrectionFileType) {
                    Patterns = ["*.txt"]
                },
                FilePickerFileTypes.All
            ]
        });
        if (!string.IsNullOrWhiteSpace(path)) {
            viewModel.LoadRoomCorrection(path);
        }
        UpdateMenuState();
    }

    void ClearFilters(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ViewModel.ClearRoomCorrection();
        UpdateMenuState();
    }

    async void DropFiles(object sender, DragEventArgs e) {
        MainViewModel viewModel = ViewModel;
        string[] paths = e.DataTransfer.TryGetFiles()?
            .Select(item => item.Path.LocalPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
        if (paths == null || paths.Length == 0) {
            return;
        }

        if (paths.Length == 1) {
            await viewModel.OpenFile(paths[0]);
        } else {
            await AddFilesToQueue(paths);
        }
    }

    async Task AddFilesToQueue(string[] paths) {
        MainViewModel viewModel = ViewModel;
        string outputFolder = null;
        if (await Confirm(Text("QuAlT", "Combined processing"), Text("QuAll",
            "Do you want to select a single output folder for all the files you're adding to the queue?"))) {
            outputFolder = await PickSingleFolderPath(new FolderPickerOpenOptions {
                Title = Text("QuAlT", "Combined processing"),
                AllowMultiple = false,
                SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory)
            });
            if (string.IsNullOrWhiteSpace(outputFolder)) {
                return;
            }
        }

        viewModel.AddFilesToQueue(paths, outputFolder);
        ExpandForQueue(viewModel);
    }

    async Task<IStorageFolder> GetStartFolder(string path) =>
        !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ?
            await StorageProvider.TryGetFolderFromPathAsync(path) :
            null;

    async Task<string> PickSingleFilePath(FilePickerOpenOptions options) {
        if (StorageProvider == null) {
            return null;
        }

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(options);
        return files.Count == 1 ? files[0].Path.LocalPath : null;
    }

    async Task<string[]> PickFilePaths(FilePickerOpenOptions options) {
        if (StorageProvider == null) {
            return [];
        }

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(options);
        return [.. files.Select(file => file.Path.LocalPath).Where(path => !string.IsNullOrWhiteSpace(path))];
    }

    async Task<string> PickSingleFolderPath(FolderPickerOpenOptions options) {
        if (StorageProvider == null) {
            return null;
        }

        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(options);
        return folders.Count == 1 ? folders[0].Path.LocalPath : null;
    }

    void ShowTextWindow(string title, string text) {
        Window dialog = new() {
            Title = title,
            Width = 700,
            Height = 500,
            MinWidth = 420,
            MinHeight = 260,
            Background = new SolidColorBrush(Color.Parse("#696969")),
            Content = new Border {
                Background = new SolidColorBrush(Color.Parse("#222222")),
                CornerRadius = new Avalonia.CornerRadius(12),
                Padding = new Avalonia.Thickness(14),
                Margin = new Avalonia.Thickness(10),
                Child = new ScrollViewer {
                    Content = new TextBlock {
                        Text = text,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.Parse("#F0F0F0")),
                        FontFamily = new FontFamily("Menlo, Consolas, monospace")
                    }
                }
            }
        };
        dialog.Show(this);
    }

    async Task<bool> Confirm(string title, string message) {
        bool result = false;
        Window dialog = new() {
            Title = title,
            Width = 480,
            Height = 210,
            MinWidth = 420,
            MinHeight = 190,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#696969"))
        };

        Button yes = new() {
            Content = "Yes",
            Width = 90
        };
        Button no = new() {
            Content = "No",
            Width = 90
        };
        yes.Click += (_, _) => {
            result = true;
            dialog.Close();
        };
        no.Click += (_, _) => dialog.Close();

        dialog.Content = new Border {
            Background = new SolidColorBrush(Color.Parse("#222222")),
            CornerRadius = new Avalonia.CornerRadius(12),
            Padding = new Avalonia.Thickness(14),
            Margin = new Avalonia.Thickness(10),
            Child = new Grid {
                RowDefinitions = new RowDefinitions("*,Auto"),
                RowSpacing = 12,
                Children = {
                    new TextBlock {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.Parse("#F0F0F0")),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    },
                    new StackPanel {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = {
                            yes,
                            no
                        }
                    }
                }
            }
        };
        Grid.SetRow(((Grid)((Border)dialog.Content).Child).Children[1], 1);
        await dialog.ShowDialog(this);
        return result;
    }

    async Task CheckForUpdates() {
        MainViewModel viewModel = ViewModel;
        if (!viewModel.CheckUpdates || DateTime.Now < viewModel.LastUpdateCheck + TimeSpan.FromDays(7)) {
            return;
        }

        string body = await Task.Run(() => HTTP.GET(updateLocation));
        if (!int.TryParse(body, out int version)) {
            return;
        }

        if (thisRevision < version &&
            await Confirm("Update available", "A new version is available! Do you want to download it?")) {
            OpenUrl(downloadLink, "Update available");
        }
        viewModel.MarkUpdateChecked();
    }

    void ExpandForQueue(MainViewModel viewModel) {
        if (viewModel.HasQueueJobs && Width < 1380) {
            Width = 1380;
        }
    }

    void ApplyViewScale() {
        double scale = ViewModel?.ViewScale ?? 1;

        Resources["HeaderFontSize"] = Scaled(26, scale);
        Resources["LabelFontSize"] = Scaled(18, scale);
        Resources["BodyFontSize"] = Scaled(16, scale);
        Resources["ButtonFontSize"] = Scaled(16, scale);
        Resources["PrimaryButtonFontSize"] = Scaled(26, scale);
        Resources["StatusFontSize"] = Scaled(22, scale);
        Resources["MenuFontSize"] = Scaled(18, scale);

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

    string Text(string key, string fallback) =>
        ViewModel.Text(key, fallback);

    string Text(string key) =>
        ViewModel.Text(key);

    string MenuText(string key, string fallback) =>
        ViewModel.MenuText(key, fallback);

    string MenuText(string key) =>
        ViewModel.MenuText(key);

    void Restart() {
        try {
            string processPath = Environment.ProcessPath;
            string appBundle = OperatingSystem.IsMacOS() ? GetAppBundle(processPath) : null;
            if (!string.IsNullOrWhiteSpace(appBundle)) {
                ProcessStartInfo startInfo = new("open") {
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

        DirectoryInfo directory = new(Path.GetDirectoryName(processPath));
        while (directory != null) {
            if (directory.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase)) {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return null;
    }

    const string updateLocation = "https://sbence.hu/ver/cavg.php";
    const string downloadLink = "https://cavern.sbence.hu/cavern/downloads.php";
    const int thisRevision = 6;
}
