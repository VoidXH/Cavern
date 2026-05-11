using Cavern.MAUI.Storage;

namespace Cavern.MAUI.Controls;

/// <summary>
/// A MAUI <see cref="Image"/> with a Save button that's visible on hover.
/// </summary>
public partial class SaveableImage : Grid {
    /// <summary>
    /// The displayed image content.
    /// </summary>
    public ImageSource Source {
        get => image.Source;
        set {
            if (value is StreamImageSource streamSource) {
                Stream stream = streamSource.Stream(CancellationToken.None).Result;
                imageFile = new byte[stream.Length];
                stream.Position = 0;
                stream.ReadAsync(imageFile, 0, imageFile.Length).Wait();
                stream.Position = 0;
            } else {
                imageFile = null;
            }
            image.Source = value;
        }
    }

    /// <summary>
    /// Scaling mode for the <see cref="image"/>.
    /// </summary>
    public Aspect Aspect {
        get => image.Aspect;
        set => image.Aspect = value;
    }

    /// <summary>
    /// The bytes making up the image to be loaded.
    /// </summary>
    byte[] imageFile;

    /// <summary>
    /// A MAUI <see cref="Image"/> with a Save button that's visible on hover.
    /// </summary>
    public SaveableImage() {
        InitializeComponent();
        PointerGestureRecognizer pointerGesture = new();
        pointerGesture.PointerEntered += (_, __) => UpdateButton(true);
        pointerGesture.PointerExited += (_, __) => UpdateButton(false);
        GestureRecognizers.Add(pointerGesture);
    }

    /// <summary>
    /// Show or hide the Save button.
    /// </summary>
    void UpdateButton(bool show) {
        saveButton.FadeToAsync(show ? 1 : 0, 250);
        saveButton.IsEnabled = show;
    }

    /// <summary>
    /// Save the displayed image to the user-selected location.
    /// </summary>
    async void OnSave(object _, EventArgs __) {
        try {
            if (imageFile == null) {
                await Shell.Current.DisplayAlertAsync("Error", "Source must be a stream.", "OK");
                return;
            }

            await new FileSaver().SaveFileAsync(imageFile, "image.png");
            await Shell.Current.DisplayAlertAsync("Success", "Image saved!", "OK");
        } catch (Exception e) {
            await Shell.Current.DisplayAlertAsync("Error", e.Message, "OK");
        }
    }
}
