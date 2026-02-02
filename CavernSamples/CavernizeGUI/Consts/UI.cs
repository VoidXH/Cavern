using System.Windows.Media;

namespace CavernizeGUI.Consts;

/// <summary>
/// Definitions for Cavern's style.
/// </summary>
sealed class UI {
    /// <summary>
    /// Color used for active speaker display.
    /// </summary>
    public static readonly SolidColorBrush activeSpeaker = new(cavernBlue);

    /// <summary>
    /// Color used for speaker display when a dynamic render target is selected.
    /// </summary>
    public static readonly SolidColorBrush dynamicSpeaker = new(Colors.Beige);

    /// <summary>
    /// Color used for inactive speaker display.
    /// </summary>
    public static readonly SolidColorBrush inactiveSpeaker = new(Colors.Gray);

    /// <summary>
    /// Cavern's signature blue.
    /// </summary>
    static readonly Color cavernBlue = Color.FromRgb(0x31, 0x86, 0xCE);
}
