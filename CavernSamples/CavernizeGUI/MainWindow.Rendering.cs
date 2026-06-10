using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace CavernizeGUI;

partial class MainWindow {
    bool renderTargetSelectorOpen;

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

    async void OnRenderTargetOpened(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (renderTargetSelectorOpen) {
            return;
        }

        renderTargetSelectorOpen = true;
        try {
            Cavernize.Logic.Models.RenderTargets.RenderTarget selected =
                await new RenderTargetSelectorWindow(ViewModel).ShowDialog<Cavernize.Logic.Models.RenderTargets.RenderTarget>(this);
            if (selected != null) {
                ViewModel.SelectedRenderTarget = selected;
            }
        } finally {
            renderTargetSelectorOpen = false;
        }
    }

    async void LocateFFmpeg(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        MainViewModel viewModel = ViewModel;
        string path = await PickSingleFilePath(new FilePickerOpenOptions {
            Title = viewModel.Text("FFLoc"),
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(viewModel.LastDirectory),
            FileTypeFilter = [FilePickerFileTypes.All]
        });
        if (!string.IsNullOrWhiteSpace(path)) {
            viewModel.SetFfmpegLocation(path);
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
}
