using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace CavernizeGUI;

partial class MainWindow {
    void Queue(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        ViewModel.AddCurrentToQueue();
        ExpandForQueue(ViewModel);
    }

    async void StartQueue(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        await ViewModel.RunQueue();
    }

    void Cancel(object sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel.Cancel();

    void RemoveQueued(object sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel.RemoveSelectedQueueJob();

    async void DropFile(object sender, DragEventArgs e) {
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
        if (await Confirm(Text("QuAlT"), Text("QuAll"))) {
            outputFolder = await PickSingleFolderPath(new FolderPickerOpenOptions {
                Title = Text("QuAlT"),
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

    void ExpandForQueue(MainViewModel viewModel) {
        if (viewModel.HasQueueJobs && Width < 1380) {
            Width = 1380;
        }
    }
}
