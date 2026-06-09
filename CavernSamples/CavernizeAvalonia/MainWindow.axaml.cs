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

    public MainWindow() => InitializeComponent();

    protected override void OnOpened(EventArgs e) {
        base.OnOpened(e);
        BuildNativeMenu();
    }

    protected override void OnClosed(EventArgs e) {
        if (DataContext is IDisposable disposable) {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }

    void BuildNativeMenu() {
        if (NativeMenu.GetMenu(this) != null) {
            return;
        }

        NativeMenu menu = new();
        NativeMenu rendering = new();
        rendering.Add(MenuCommand("Upmixing setup", OpenUpmixSetup,
            "Use techniques to create 7.1 from smaller mixes or give height to old channel-based mixes."));
        rendering.Add(MenuCommand("Load HRTF/HRIR sets for the Virtualizer", (_, _) => LoadHrir(null, null),
            "Override Cavern's own filters used for Headphone Virtualizer with a multichannel WAV."));
        speakerVirtualizerMenuItem = CheckMenuCommand("Height virtualization on speakers",
            viewModel => viewModel.SpeakerVirtualizer, (viewModel, value) => viewModel.SpeakerVirtualizer = value,
            "Uses the Headphone Virtualizer's filters to render the heights to main channels.");
        rendering.Add(speakerVirtualizerMenuItem);
        rendering.Add(MenuCommand("Apply output filters", (_, _) => LoadFilters(null, null),
            "Parses a Cavern QuickEQ convolution export for the target system to be used as an equalizer."));
        rendering.Add(new NativeMenuItemSeparator());
        muteBedMenuItem = CheckMenuCommand("Mute bed",
            viewModel => viewModel.MuteBed, (viewModel, value) => viewModel.MuteBed = value,
            "Silence all objects that's at the position of a reference channel.");
        rendering.Add(muteBedMenuItem);
        muteGroundMenuItem = CheckMenuCommand("Mute ground",
            viewModel => viewModel.MuteGround, (viewModel, value) => viewModel.MuteGround = value,
            "Silence all objects on the ground, including the ones that move.");
        rendering.Add(muteGroundMenuItem);
        rendering.Add(new NativeMenuItemSeparator());
        force24BitMenuItem = CheckMenuCommand("Force 24-bit PCM",
            viewModel => viewModel.Force24Bit, (viewModel, value) => viewModel.Force24Bit = value,
            "Use 24-bit PCM output for supported formats.");
        rendering.Add(force24BitMenuItem);
        surroundSwapMenuItem = CheckMenuCommand("Swap side/rear output channels",
            viewModel => viewModel.SurroundSwap, (viewModel, value) => viewModel.SurroundSwap = value,
            "Swap what is connected to the side and rear output pairs.");
        rendering.Add(surroundSwapMenuItem);
        wavChannelSkipMenuItem = CheckMenuCommand("Skip RIFF WAVE channel mask",
            viewModel => viewModel.WavChannelSkip, (viewModel, value) => viewModel.WavChannelSkip = value,
            "Don't export the channel mapping to PCM files, and allow unsupported channels.");
        rendering.Add(wavChannelSkipMenuItem);
        rendering.Add(new NativeMenuItemSeparator());
        rendering.Add(MenuCommand("Show metadata", (_, _) => ShowMetadata(null, null),
            "Display what data was parsed from the format header of the currently open audio file."));
        reportModeMenuItem = CheckMenuCommand("Report only mode",
            viewModel => viewModel.ReportMode, (viewModel, value) => viewModel.ReportMode = value,
            "Don't export to file, just virtually perform the processing.");
        rendering.Add(reportModeMenuItem);
        detailedGradingMenuItem = CheckMenuCommand("Quality analysis and grading",
            viewModel => viewModel.DetailedGrading, (viewModel, value) => viewModel.DetailedGrading = value,
            "In addition to rendering, grade the quality of the processed audio.");
        rendering.Add(detailedGradingMenuItem);
        rendering.Add(MenuCommand("Show post-render report", (_, _) => ShowPostRenderReport(null, null),
            "If quality analysis and grading was enabled, display its results after rendering."));

        NativeMenu language = new();
        language.Add(new NativeMenuItem("English") {
            IsEnabled = false
        });
        language.Add(new NativeMenuItem("Magyar") {
            IsEnabled = false
        });

        NativeMenu help = new();
        help.Add(MenuCommand("User guide", (_, _) => OpenUserGuide(null, null)));
        help.Add(MenuCommand("About", (_, _) => ShowAbout(null, null)));

        menu.Add(new NativeMenuItem("Rendering") {
            Menu = rendering
        });
        menu.Add(new NativeMenuItem("Language") {
            Menu = language
        });
        menu.Add(new NativeMenuItem("Help") {
            Menu = help
        });
        menu.NeedsUpdate += (_, _) => UpdateNativeMenuState();
        NativeMenu.SetMenu(this, menu);
        UpdateNativeMenuState();
    }

    NativeMenuItem MenuCommand(string header, EventHandler click, string toolTip = null) {
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
            if (DataContext is MainViewModel viewModel) {
                bool value = !getter(viewModel);
                setter(viewModel, value);
                item.IsChecked = value;
            }
        };
        return item;
    }

    void UpdateNativeMenuState() {
        if (DataContext is not MainViewModel viewModel) {
            return;
        }

        speakerVirtualizerMenuItem.IsChecked = viewModel.SpeakerVirtualizer;
        muteBedMenuItem.IsChecked = viewModel.MuteBed;
        muteGroundMenuItem.IsChecked = viewModel.MuteGround;
        force24BitMenuItem.IsChecked = viewModel.Force24Bit;
        surroundSwapMenuItem.IsChecked = viewModel.SurroundSwap;
        surroundSwapMenuItem.IsEnabled = !viewModel.SelectedExportFormat.Codec.IsEnvironmental();
        wavChannelSkipMenuItem.IsChecked = viewModel.WavChannelSkip;
        reportModeMenuItem.IsChecked = viewModel.ReportMode;
        detailedGradingMenuItem.IsChecked = viewModel.DetailedGrading;
    }

    async void OpenUpmixSetup(object sender, EventArgs e) {
        if (DataContext is not MainViewModel viewModel) {
            return;
        }

        UpmixingSetupWindow dialog = new(viewModel);
        await dialog.ShowDialog(this);
        if (dialog.Accepted) {
            dialog.ApplyTo(viewModel);
            UpdateNativeMenuState();
        }
    }

    async void OpenFile(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is not MainViewModel viewModel || StorageProvider == null) {
            return;
        }

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Open source",
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
            FileTypeFilter = [
                new FilePickerFileType("Audio and video") {
                    Patterns = Cavern.Format.AudioReader.filter.Split(';')
                },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count != 1) {
            return;
        }

        string path = files[0].Path.LocalPath;
        if (!string.IsNullOrWhiteSpace(path)) {
            await viewModel.OpenFile(path);
        }
    }

    async void RenderFile(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is not MainViewModel viewModel || StorageProvider == null) {
            return;
        }

        string path = null;
        if (!viewModel.ReportMode) {
            IStorageFile file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
                Title = "Save render",
                SuggestedFileName = viewModel.SuggestedOutputName,
                DefaultExtension = viewModel.SuggestedOutputExtension,
                SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
                FileTypeChoices = [
                    new FilePickerFileType("Selected format") {
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
        if (DataContext is MainViewModel viewModel) {
            viewModel.AddCurrentToQueue();
            ExpandForQueue(viewModel);
        }
    }

    async void RunQueue(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is MainViewModel viewModel) {
            await viewModel.RunQueue();
        }
    }

    void Cancel(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is MainViewModel viewModel) {
            viewModel.Cancel();
        }
    }

    void RemoveQueued(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is MainViewModel viewModel) {
            viewModel.RemoveSelectedQueueJob();
        }
    }

    void ShowWiring(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is MainViewModel viewModel) {
            ShowTextWindow("Display wiring", viewModel.GetWiringText());
        }
    }

    void ShowSystemInfo(object sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ShowTextWindow("System", "Choose a layout, and place your speakers accordingly. Click the \"Display wiring\" button " +
            "to see which output will change to which actual channel.\n\nFor maximum audio quality, calibrate your system with QuickEQ.");

    async void LocateFFmpeg(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is not MainViewModel viewModel || StorageProvider == null) {
            return;
        }

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Locate FFmpeg",
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
            FileTypeFilter = [FilePickerFileTypes.All]
        });

        if (files.Count == 1) {
            string path = files[0].Path.LocalPath;
            if (!string.IsNullOrWhiteSpace(path)) {
                viewModel.SetFfmpegLocation(path);
            }
        }
    }

    void ShowMetadata(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is MainViewModel viewModel) {
            ShowTextWindow("Codec metadata", viewModel.GetMetadataText());
        }
    }

    void ShowPostRenderReport(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is MainViewModel viewModel) {
            ShowTextWindow("Post-render report", viewModel.GetPostRenderReportText());
        }
    }

    void OpenUserGuide(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        try {
            Process.Start(new ProcessStartInfo("https://cavern.sbence.hu/cavern/doc.php") {
                UseShellExecute = true
            });
        } catch {
            if (DataContext is MainViewModel viewModel) {
                ShowTextWindow("User guide", "https://cavern.sbence.hu/cavern/doc.php");
            }
        }
    }

    void ShowAbout(object sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ShowTextWindow("About", "Cavernize\nCopyright (C) Bence Sganetz 2016-2026\nCross-platform Avalonia macOS port.");

    async void LoadHrir(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is not MainViewModel viewModel || StorageProvider == null) {
            return;
        }

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Load HRIR",
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
            FileTypeFilter = [
                new FilePickerFileType("Impulse response packages") {
                    Patterns = ["*.wav"]
                },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count == 1) {
            string path = files[0].Path.LocalPath;
            if (!string.IsNullOrWhiteSpace(path)) {
                await viewModel.LoadHrir(path);
            }
        }
    }

    void ResetHrir(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is MainViewModel viewModel) {
            viewModel.ResetHrir();
        }
    }

    async void LoadFilters(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is not MainViewModel viewModel || StorageProvider == null) {
            return;
        }

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Load room correction filters",
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastFilterDirectory),
            FileTypeFilter = [
                new FilePickerFileType("Cavern QuickEQ convolution EQs") {
                    Patterns = ["*.txt"]
                },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count == 1) {
            string path = files[0].Path.LocalPath;
            if (!string.IsNullOrWhiteSpace(path)) {
                viewModel.LoadRoomCorrection(path);
            }
        }
    }

    void ClearFilters(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (DataContext is MainViewModel viewModel) {
            viewModel.ClearRoomCorrection();
        }
    }

    async void DropFiles(object sender, DragEventArgs e) {
        if (DataContext is not MainViewModel viewModel) {
            return;
        }

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
}
