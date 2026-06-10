using Avalonia.Platform.Storage;

namespace CavernizeGUI;

// Functions that are part of the render export process.
partial class MainWindow {
    async void Render(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
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
}
