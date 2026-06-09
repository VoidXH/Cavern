using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Cavern.Format.Common;
using System.Diagnostics;

namespace CavernizeAvalonia;

public partial class MainWindow : Window {
    NativeMenuItem speakerVirtualizerMenuItem;
    NativeMenuItem muteBedMenuItem;
    NativeMenuItem muteGroundMenuItem;
    NativeMenuItem force24BitMenuItem;
    NativeMenuItem surroundSwapMenuItem;
    NativeMenuItem wavChannelSkipMenuItem;
    NativeMenuItem reportModeMenuItem;
    NativeMenuItem detailedGradingMenuItem;
    NativeMenuItem englishLanguageMenuItem;
    NativeMenuItem hungarianLanguageMenuItem;

    public MainWindow() => InitializeComponent();

    MainViewModel ViewModel => (MainViewModel)DataContext;

    protected override void OnOpened(EventArgs e) {
        base.OnOpened(e);
        BuildNativeMenu();
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
        NativeMenu rendering = new();
        rendering.Add(MenuCommand(MenuText("Upmix", "Upmixing setup..."), OpenUpmixSetup,
            "Use techniques to create 7.1 from smaller mixes or give height to old channel-based mixes."));
        rendering.Add(MenuCommand(MenuText("LoadV", "Load HRTF/HRIR sets for the Virtualizer"), (_, _) => LoadHrir(null, null),
            "Override Cavern's own filters used for Headphone Virtualizer with a multichannel WAV."));
        speakerVirtualizerMenuItem = CheckMenuCommand(MenuText("SpVir", "Height virtualization on speakers"),
            viewModel => viewModel.SpeakerVirtualizer, (viewModel, value) => viewModel.SpeakerVirtualizer = value,
            Text("SpVirT", "Uses the Headphone Virtualizer's filters to render the heights to main channels."));
        rendering.Add(speakerVirtualizerMenuItem);
        rendering.Add(MenuCommand(MenuText("FiltH", "Apply output filters"), (_, _) => LoadFilters(null, null),
            Text("FiltT", "Parses a Cavern QuickEQ convolution export for the target system to be used as an equalizer.")));
        rendering.Add(new NativeMenuItemSeparator());
        muteBedMenuItem = CheckMenuCommand(MenuText("MuBeH", "Mute bed"),
            viewModel => viewModel.MuteBed, (viewModel, value) => viewModel.MuteBed = value,
            Text("MuBeT", "Silence all objects that's at the position of a reference channel."));
        rendering.Add(muteBedMenuItem);
        muteGroundMenuItem = CheckMenuCommand(MenuText("MuGrH", "Mute ground"),
            viewModel => viewModel.MuteGround, (viewModel, value) => viewModel.MuteGround = value,
            Text("MuGrT", "Silence all objects on the ground, including the ones that move."));
        rendering.Add(muteGroundMenuItem);
        rendering.Add(new NativeMenuItemSeparator());
        force24BitMenuItem = CheckMenuCommand(MenuText("For24", "Force 24-bit PCM"),
            viewModel => viewModel.Force24Bit, (viewModel, value) => viewModel.Force24Bit = value,
            "Use 24-bit PCM output for supported formats.");
        rendering.Add(force24BitMenuItem);
        surroundSwapMenuItem = CheckMenuCommand(MenuText("SuSwa", "Swap side/rear output channels"),
            viewModel => viewModel.SurroundSwap, (viewModel, value) => viewModel.SurroundSwap = value,
            "Swap what is connected to the side and rear output pairs.");
        rendering.Add(surroundSwapMenuItem);
        wavChannelSkipMenuItem = CheckMenuCommand(MenuText("WavCh", "Skip RIFF WAVE channel mask"),
            viewModel => viewModel.WavChannelSkip, (viewModel, value) => viewModel.WavChannelSkip = value,
            "Don't export the channel mapping to PCM files, and allow unsupported channels.");
        rendering.Add(wavChannelSkipMenuItem);
        rendering.Add(new NativeMenuItemSeparator());
        rendering.Add(MenuCommand(MenuText("SMetH", "Show metadata..."), (_, _) => ShowMetadata(null, null),
            Text("SMetT", "Display what data was parsed from the format header of the currently open audio file.")));
        reportModeMenuItem = CheckMenuCommand(MenuText("ReMoH", "Report only mode"),
            viewModel => viewModel.ReportMode, (viewModel, value) => viewModel.ReportMode = value,
            Text("ReMoT", "Don't export to file, just virtually perform the processing."));
        rendering.Add(reportModeMenuItem);
        detailedGradingMenuItem = CheckMenuCommand(MenuText("DeGrH", "Quality analysis and grading"),
            viewModel => viewModel.DetailedGrading, (viewModel, value) => viewModel.DetailedGrading = value,
            Text("DeGrT", "In addition to rendering, grade the quality of the processed audio."));
        rendering.Add(detailedGradingMenuItem);
        rendering.Add(MenuCommand(MenuText("PReSh", "Show post-render report..."), (_, _) => ShowPostRenderReport(null, null),
            "If quality analysis and grading was enabled, display its results after rendering."));

        NativeMenu language = new();
        englishLanguageMenuItem = LanguageMenuCommand(MenuText("LanEn", "English"), "en-US");
        language.Add(englishLanguageMenuItem);
        hungarianLanguageMenuItem = LanguageMenuCommand(MenuText("LanHu", "Magyar"), "hu-HU");
        language.Add(hungarianLanguageMenuItem);

        NativeMenu help = new();
        help.Add(MenuCommand(MenuText("UsrGu", "User guide"), (_, _) => OpenUserGuide(null, null)));
        help.Add(MenuCommand(MenuText("About", "About"), (_, _) => ShowAbout(null, null)));

        menu.Add(new NativeMenuItem(MenuText("MenuR", "Rendering")) {
            Menu = rendering
        });
        menu.Add(new NativeMenuItem(MenuText("MenuL", "Language")) {
            Menu = language
        });
        menu.Add(new NativeMenuItem(MenuText("MenuH", "Help")) {
            Menu = help
        });
        menu.NeedsUpdate += (_, _) => UpdateNativeMenuState();
        NativeMenu.SetMenu(this, menu);
        UpdateNativeMenuState();
    }

    NativeMenuItem MenuCommand(string header, EventHandler click) => MenuCommand(header, click, null);

    NativeMenuItem MenuCommand(string header, EventHandler click, string toolTip) {
        NativeMenuItem item = new(header) {
            ToolTip = toolTip
        };
        item.Click += click;
        return item;
    }

    NativeMenuItem CheckMenuCommand(string header, Func<MainViewModel, bool> getter, Action<MainViewModel, bool> setter,
        string toolTip) {
        NativeMenuItem item = new(header) {
            ToggleType = MenuItemToggleType.CheckBox,
            ToolTip = toolTip
        };
        item.Click += (_, _) => {
            MainViewModel viewModel = ViewModel;
            bool value = !getter(viewModel);
            setter(viewModel, value);
            item.IsChecked = value;
        };
        return item;
    }

    NativeMenuItem LanguageMenuCommand(string header, string code) {
        NativeMenuItem item = new(header) {
            ToggleType = MenuItemToggleType.Radio
        };
        item.Click += (_, _) => {
            if (ViewModel.SetLanguage(code)) {
                Restart();
            } else {
                UpdateNativeMenuState();
            }
        };
        return item;
    }

    void UpdateNativeMenuState() {
        MainViewModel viewModel = ViewModel;

        speakerVirtualizerMenuItem.IsChecked = viewModel.SpeakerVirtualizer;
        muteBedMenuItem.IsChecked = viewModel.MuteBed;
        muteGroundMenuItem.IsChecked = viewModel.MuteGround;
        force24BitMenuItem.IsChecked = viewModel.Force24Bit;
        surroundSwapMenuItem.IsChecked = viewModel.SurroundSwap;
        surroundSwapMenuItem.IsEnabled = !viewModel.SelectedExportFormat.Codec.IsEnvironmental();
        wavChannelSkipMenuItem.IsChecked = viewModel.WavChannelSkip;
        reportModeMenuItem.IsChecked = viewModel.ReportMode;
        detailedGradingMenuItem.IsChecked = viewModel.DetailedGrading;
        englishLanguageMenuItem.IsChecked = viewModel.LanguageCode == "en-US";
        hungarianLanguageMenuItem.IsChecked = viewModel.LanguageCode == "hu-HU";
    }

    async void OpenUpmixSetup(object sender, EventArgs e) {
        MainViewModel viewModel = ViewModel;
        UpmixingSetupWindow dialog = new(viewModel);
        await dialog.ShowDialog(this);
        if (dialog.Accepted) {
            dialog.ApplyTo(viewModel);
            UpdateNativeMenuState();
        }
    }

    async void OpenFile(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        MainViewModel viewModel = ViewModel;
        string path = await PickSingleFilePath(new FilePickerOpenOptions {
            Title = viewModel.OpenSourcePickerTitle,
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
            FileTypeFilter = [
                new FilePickerFileType(viewModel.AudioVideoFileType) {
                    Patterns = Cavern.Format.AudioReader.filter.Split(';')
                },
                FilePickerFileTypes.All
            ]
        });
        if (!string.IsNullOrWhiteSpace(path)) {
            await viewModel.OpenFile(path);
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
        ShowTextWindow(ViewModel.Text("CMetT", "Codec metadata"), ViewModel.GetMetadataText());
    }

    void ShowPostRenderReport(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ShowTextWindow(ViewModel.Text("PReRe", "Post-render report"), ViewModel.GetPostRenderReportText());
    }

    void OpenUserGuide(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        try {
            Process.Start(new ProcessStartInfo("https://cavern.sbence.hu/cavern/doc.php") {
                UseShellExecute = true
            });
        } catch {
            ShowTextWindow(Text("UsrGu", "User guide"), "https://cavern.sbence.hu/cavern/doc.php");
        }
    }

    void ShowAbout(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ShowTextWindow(ViewModel.Text("AbouH", "About"), "Cavernize\nCopyright (C) Bence Sganetz 2016-2026\n" +
            $"{ViewModel.Text("AbouA", "Performance accelerated with CavernAmp.")}\nCross-platform Avalonia macOS port.");
    }

    async void LoadHrir(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
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
    }

    void ResetHrir(object sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel.ResetHrir();

    async void LoadFilters(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
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
    }

    void ClearFilters(object sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel.ClearRoomCorrection();

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
            viewModel.AddFilesToQueue(paths);
            ExpandForQueue(viewModel);
        }
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

    void ExpandForQueue(MainViewModel viewModel) {
        if (viewModel.HasQueueJobs && Width < 1380) {
            Width = 1380;
        }
    }

    string Text(string key, string fallback) =>
        ViewModel.Text(key, fallback);

    string MenuText(string key, string fallback) =>
        ViewModel.MenuText(key, fallback);

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
}
