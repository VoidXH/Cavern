using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace CavernizeGUI;

partial class MainWindow {
    /// <summary>
    /// Queue a rendering process.
    /// </summary>
    void Queue(object _, Avalonia.Interactivity.RoutedEventArgs e) {
        AddCurrentToQueue();
        ExpandForQueue();
    }

    /// <summary>
    /// Start processing the queue.
    /// </summary>
    async void StartQueue(object _, Avalonia.Interactivity.RoutedEventArgs e) {
        await RunQueue();
    }

    void Cancel(object _, Avalonia.Interactivity.RoutedEventArgs e) => Cancel();

    /// <summary>
    /// Removes a queued job.
    /// </summary>
    void RemoveQueued(object _, Avalonia.Interactivity.RoutedEventArgs e) => RemoveSelectedQueueJob();

    /// <summary>
    /// Handle when files are dropped on the list of queued jobs.
    /// </summary>
    async void DropFile(object _, DragEventArgs e) {
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

    /// <summary>
    /// Add files to the queue, prompting the user to select a single folder where all output files will be written
    /// in the current configuration's default container.
    /// </summary>
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
