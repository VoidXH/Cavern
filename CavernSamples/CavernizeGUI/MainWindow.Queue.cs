using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace CavernizeGUI;

partial class MainWindow {
    void Queue(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        AddCurrentToQueue();
        ExpandForQueue();
    }

    async void StartQueue(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        await RunQueue();
    }

    void Cancel(object sender, Avalonia.Interactivity.RoutedEventArgs e) => Cancel();

    void RemoveQueued(object sender, Avalonia.Interactivity.RoutedEventArgs e) => RemoveSelectedQueueJob();

    async void DropFile(object sender, DragEventArgs e) {
        string[] paths = e.DataTransfer.TryGetFiles()?
            .Select(item => item.Path.LocalPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
        if (paths == null || paths.Length == 0) {
            return;
        }

        if (paths.Length == 1) {
            await OpenFile(paths[0]);
        } else {
            await AddFilesToQueue(paths);
        }
    }

    async Task AddFilesToQueue(string[] paths) {
        string outputFolder = null;
        if (await Confirm(Text("QuAlT"), Text("QuAll"))) {
            outputFolder = await PickSingleFolderPath(new FolderPickerOpenOptions {
                Title = Text("QuAlT"),
                AllowMultiple = false,
                SuggestedStartLocation = await GetStartFolder(LastDirectory)
            });
            if (string.IsNullOrWhiteSpace(outputFolder)) {
                return;
            }
        }

        AddFilesToQueue(paths, outputFolder);
        ExpandForQueue();
    }

    void ExpandForQueue() {
        if (HasQueueJobs && Width < 1380) {
            Width = 1380;
        }
    }
}
