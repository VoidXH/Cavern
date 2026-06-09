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

namespace CavernizeAvalonia;

public partial class MainWindow : Window {
    NativeMenuItem speakerVirtualizerMenuItem;
    NativeMenuItem hrirMenuItem;
    NativeMenuItem filtersMenuItem;
    NativeMenuItem muteBedMenuItem;
    NativeMenuItem muteGroundMenuItem;
    NativeMenuItem force24BitMenuItem;
    NativeMenuItem surroundSwapMenuItem;
    NativeMenuItem wavChannelSkipMenuItem;
    NativeMenuItem reportModeMenuItem;
    NativeMenuItem detailedGradingMenuItem;
    NativeMenuItem zoom50MenuItem;
    NativeMenuItem zoom75MenuItem;
    NativeMenuItem zoom100MenuItem;
    NativeMenuItem zoom125MenuItem;
    NativeMenuItem englishLanguageMenuItem;
    NativeMenuItem hungarianLanguageMenuItem;
    NativeMenuItem checkUpdatesMenuItem;

    public MainWindow() => InitializeComponent();

    MainViewModel ViewModel => (MainViewModel)DataContext;

    protected override void OnOpened(EventArgs e) {
        base.OnOpened(e);
        ApplyViewScale();
        BuildNativeMenu();
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
        NativeMenu rendering = new();
        rendering.Add(MenuCommand(MenuText("Upmix", "Upmixing setup..."), OpenUpmixSetup,
            "Use techniques to create 7.1 from smaller mixes or give height to old channel-based mixes."));
        hrirMenuItem = MenuCommand(MenuText("LoadV", "Load HRTF/HRIR sets for the Virtualizer"), ToggleHrir,
            "Override Cavern's own filters used for Headphone Virtualizer with a multichannel WAV.");
        hrirMenuItem.ToggleType = MenuItemToggleType.CheckBox;
        rendering.Add(hrirMenuItem);
        speakerVirtualizerMenuItem = CheckMenuCommand(MenuText("SpVir", "Height virtualization on speakers"),
            viewModel => viewModel.SpeakerVirtualizer, (viewModel, value) => viewModel.SpeakerVirtualizer = value,
            Text("SpVirT", "Uses the Headphone Virtualizer's filters to render the heights to main channels."));
        rendering.Add(speakerVirtualizerMenuItem);
        filtersMenuItem = MenuCommand(MenuText("FiltH", "Apply output filters"), ToggleFilters,
            Text("FiltT", "Parses a Cavern QuickEQ convolution export for the target system to be used as an equalizer."));
        filtersMenuItem.ToggleType = MenuItemToggleType.CheckBox;
        rendering.Add(filtersMenuItem);
        rendering.Add(MenuCommand(Text("FFLoc", "Locate FFmpeg"), (_, _) => LocateFFmpeg(null, null),
            Text("FFDes", "Locate FFmpeg with this menu item.")));
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

        NativeMenu view = new();
        zoom50MenuItem = ZoomMenuCommand("50%", .5);
        view.Add(zoom50MenuItem);
        zoom75MenuItem = ZoomMenuCommand("75%", .75);
        view.Add(zoom75MenuItem);
        zoom100MenuItem = ZoomMenuCommand("100%", 1);
        view.Add(zoom100MenuItem);
        zoom125MenuItem = ZoomMenuCommand("125%", 1.25);
        view.Add(zoom125MenuItem);

        NativeMenu language = new();
        englishLanguageMenuItem = LanguageMenuCommand(MenuText("LanEn", "English"), "en-US");
        language.Add(englishLanguageMenuItem);
        hungarianLanguageMenuItem = LanguageMenuCommand(MenuText("LanHu", "Magyar"), "hu-HU");
        language.Add(hungarianLanguageMenuItem);

        NativeMenu help = new();
        checkUpdatesMenuItem = CheckMenuCommand(MenuText("ChkUp", "Check for updates"),
            viewModel => viewModel.CheckUpdates, (viewModel, value) => viewModel.CheckUpdates = value,
            Text("ChkTt", "Check for new Cavernize releases once a week."));
        help.Add(checkUpdatesMenuItem);
        help.Add(new NativeMenuItemSeparator());
        help.Add(MenuCommand(MenuText("UsrGu", "User guide"), (_, _) => OpenUserGuide(null, null)));
        help.Add(MenuCommand(MenuText("About", "About"), (_, _) => ShowAbout(null, null)));

        menu.Add(new NativeMenuItem(MenuText("MenuR", "Rendering")) {
            Menu = rendering
        });
        menu.Add(new NativeMenuItem("View") {
            Menu = view
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

    NativeMenuItem ZoomMenuCommand(string header, double scale) {
        NativeMenuItem item = new(header) {
            ToggleType = MenuItemToggleType.Radio
        };
        item.Click += (_, _) => {
            ViewModel.ViewScale = scale;
            ApplyViewScale();
            UpdateNativeMenuState();
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
        hrirMenuItem.IsChecked = viewModel.HasHrir;
        filtersMenuItem.IsChecked = viewModel.HasRoomCorrection;
        muteBedMenuItem.IsChecked = viewModel.MuteBed;
        muteGroundMenuItem.IsChecked = viewModel.MuteGround;
        force24BitMenuItem.IsChecked = viewModel.Force24Bit;
        surroundSwapMenuItem.IsChecked = viewModel.SurroundSwap;
        surroundSwapMenuItem.IsEnabled = !viewModel.SelectedExportFormat.Codec.IsEnvironmental();
        wavChannelSkipMenuItem.IsChecked = viewModel.WavChannelSkip;
        reportModeMenuItem.IsChecked = viewModel.ReportMode;
        detailedGradingMenuItem.IsChecked = viewModel.DetailedGrading;
        zoom50MenuItem.IsChecked = Math.Abs(viewModel.ViewScale - .5) < .001;
        zoom75MenuItem.IsChecked = Math.Abs(viewModel.ViewScale - .75) < .001;
        zoom100MenuItem.IsChecked = Math.Abs(viewModel.ViewScale - 1) < .001;
        zoom125MenuItem.IsChecked = Math.Abs(viewModel.ViewScale - 1.25) < .001;
        englishLanguageMenuItem.IsChecked = viewModel.LanguageCode == "en-US";
        hungarianLanguageMenuItem.IsChecked = viewModel.LanguageCode == "hu-HU";
        checkUpdatesMenuItem.IsChecked = viewModel.CheckUpdates;
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

    async void OpenRenderTargetSelector(object sender, EventArgs e) {
        renderTarget.IsDropDownOpen = false;
        RenderTarget selected = await new RenderTargetSelectorWindow(ViewModel).ShowDialog<RenderTarget>(this);
        if (selected != null) {
            ViewModel.SelectedRenderTarget = selected;
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
        FileInfo cavernizeAvalonia = new(Path.Combine(AppContext.BaseDirectory, "CavernizeAvalonia.dll"));
        result.Append(cavernizeLogic.Exists ? cavernizeLogic.CreationTime : "unknown").Append(", ")
            .Append(cavernizeAvalonia.Exists ? cavernizeAvalonia.CreationTime : "unknown");
        ShowTextWindow(ViewModel.Text("AbouH", "About"), result.ToString());
    }

    async void ToggleHrir(object sender, EventArgs e) {
        if (ViewModel.HasHrir) {
            ViewModel.ResetHrir();
        } else {
            await LoadHrir();
        }
        UpdateNativeMenuState();
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
        UpdateNativeMenuState();
    }

    void ResetHrir(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ViewModel.ResetHrir();
        UpdateNativeMenuState();
    }

    async void ToggleFilters(object sender, EventArgs e) {
        if (ViewModel.HasRoomCorrection) {
            ViewModel.ClearRoomCorrection();
        } else {
            await LoadFilters();
        }
        UpdateNativeMenuState();
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
        UpdateNativeMenuState();
    }

    void ClearFilters(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ViewModel.ClearRoomCorrection();
        UpdateNativeMenuState();
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

        Resources["WorkspaceMinWidth"] = Scaled(930, scale);
        Resources["WorkspaceMinHeight"] = Scaled(590, scale);
        Resources["SystemColumnWidth"] = new GridLength(Scaled(360, scale));
        Resources["ContentColumnMinWidth"] = Scaled(500, scale);
        Resources["QueueColumnWidth"] = Scaled(365, scale);

        Resources["OuterPadding"] = new Thickness(Scaled(15, scale));
        Resources["SystemCardPadding"] = new Thickness(Scaled(20, scale));
        Resources["ContentCardPadding"] = new Thickness(Scaled(15, scale));
        Resources["StatusPadding"] = new Thickness(Scaled(20, scale), Scaled(10, scale));
        Resources["ButtonPadding"] = new Thickness(Scaled(14, scale), Scaled(8, scale));
        Resources["ComboPadding"] = new Thickness(Scaled(8, scale), Scaled(2, scale));
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
        Resources["QueueButtonMinWidth"] = Scaled(155, scale);
        Resources["QueueRunButtonMinWidth"] = Scaled(130, scale);
        Resources["QueueProgressHeight"] = Scaled(8, scale);
        Resources["StatusProgressWidth"] = Scaled(225, scale);
        Resources["StatusProgressHeight"] = Scaled(30, scale);
    }

    static double Scaled(double value, double scale) => Math.Round(value * scale, 2);

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

    const string updateLocation = "https://sbence.hu/ver/cavg.php";
    const string downloadLink = "https://cavern.sbence.hu/cavern/downloads.php";
    const int thisRevision = 6;
}
