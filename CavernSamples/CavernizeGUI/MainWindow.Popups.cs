using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Cavern;
using Cavern.Format.Common;
using Cavern.Utilities;
using System.Diagnostics;
using System.Text;
using VoidX.WPF;

namespace CavernizeGUI;

partial class MainWindow {
    async void OpenUpmixSetup(object sender, EventArgs e) {
        MainViewModel viewModel = ViewModel;
        UpmixingSetupWindow dialog = new(viewModel);
        await dialog.ShowDialog(this);
        if (dialog.Accepted) {
            dialog.ApplyTo(viewModel);
            UpdateMenuState();
        }
    }

    async void ToggleHrir(object sender, EventArgs e) {
        if (ViewModel.HasHrir) {
            ViewModel.ResetHrir();
        } else {
            await LoadHRIR();
        }
        UpdateMenuState();
    }

    async void LoadHRIR(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        await LoadHRIR();
    }

    async Task LoadHRIR() {
        MainViewModel viewModel = ViewModel;
        string path = await PickSingleFilePath(new Avalonia.Platform.Storage.FilePickerOpenOptions {
            Title = viewModel.LoadHrirTitle,
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
            FileTypeFilter = [
                new Avalonia.Platform.Storage.FilePickerFileType(viewModel.ImpulseResponseFileType) {
                    Patterns = ["*.wav"]
                },
                Avalonia.Platform.Storage.FilePickerFileTypes.All
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
        string path = await PickSingleFilePath(new Avalonia.Platform.Storage.FilePickerOpenOptions {
            Title = viewModel.LoadFiltersTitle,
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastFilterDirectory),
            FileTypeFilter = [
                new Avalonia.Platform.Storage.FilePickerFileType(viewModel.RoomCorrectionFileType) {
                    Patterns = ["*.txt"]
                },
                Avalonia.Platform.Storage.FilePickerFileTypes.All
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

    void DisplayWiring(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ShowTextWindow(ViewModel.DisplayWiringText, ViewModel.GetWiringText());
    }

    void ShowSystemInfo(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ShowTextWindow(ViewModel.SystemTitle, ViewModel.SystemInfoText);
    }

    void ShowMetadata(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        string title = ViewModel.Text("CMetT");
        if (ViewModel.SelectedTrack == null) {
            ShowTextWindow(title, ViewModel.Text("CMeET"));
            return;
        }

        ReadableMetadata metadata = ViewModel.SelectedTrack.GetMetadata();
        if (metadata == null) {
            ShowTextWindow(title, ViewModel.Text("CMeUT"));
            return;
        }

        new MetadataWindow(metadata, title).Show(this);
    }

    void ShowPostRenderReport(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ShowTextWindow(ViewModel.Text("PReRe"), ViewModel.GetPostRenderReportText());
    }

    void Guide(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        OpenUrl("https://cavern.sbence.hu/cavern/doc.php?p=Cavernize", Text("UsrGu"));
    }

    void Ad(object sender, PointerPressedEventArgs e) => OpenUrl("https://cavern.sbence.hu", "Cavern");

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
            result.Append('\n').Append(ViewModel.Text("AbouA"));
        }

        result.AppendLine().Append("Build: ");
        FileInfo cavernizeLogic = new(Path.Combine(AppContext.BaseDirectory, "Cavernize.Logic.dll"));
        FileInfo cavernizeGui = new(Path.Combine(AppContext.BaseDirectory, "CavernizeGUI.dll"));
        result.Append(cavernizeLogic.Exists ? cavernizeLogic.CreationTime : "unknown").Append(", ")
            .Append(cavernizeGui.Exists ? cavernizeGui.CreationTime : "unknown");
        ShowTextWindow(ViewModel.Text("AbouH"), result.ToString());
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
            Content = Text("Yes"),
            Width = 90
        };
        Button no = new() {
            Content = Text("No"),
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
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new StackPanel {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
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

        if (thisRevision < version && await Confirm(Text("UpdAv"), Text("UpdQu"))) {
            OpenUrl(downloadLink, Text("UpdAv"));
        }
        viewModel.MarkUpdateChecked();
    }

    const string updateLocation = "https://sbence.hu/ver/cavg.php";
    const string downloadLink = "https://cavern.sbence.hu/cavern/downloads.php";
    const int thisRevision = 6;
}
