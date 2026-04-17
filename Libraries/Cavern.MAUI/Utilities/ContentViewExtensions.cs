namespace Cavern.MAUI.Utilities;

/// <summary>
/// Functions that should have been present in <see cref="ContentView"/>s.
/// </summary>
public static class ContentViewExtensions {
    /// <summary>
    /// Show an alert dialog with a single <paramref name="cancel"/> button (of which the text is its argument).
    /// </summary>
    public static Task DisplayAlertAsync(this ContentView view, string title, string message, string cancel) {
        if (view.Parent is not Page page) {
            return Task.CompletedTask;
        }
        return page.DisplayAlertAsync(title, message, cancel);
    }
}
