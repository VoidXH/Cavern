using Cavern.Utilities;

namespace CavernizeAvalonia;

/// <summary>
/// Licence prompt implementation for non-interactive contexts.
/// </summary>
public sealed class RejectingLicence : ILicence {
    /// <summary>
    /// User-facing explanation for the requested external software.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Licence text that would have to be accepted.
    /// </summary>
    public string LicenceText { get; private set; }

    /// <inheritdoc/>
    public void SetDescription(string description) => Description = description;

    /// <inheritdoc/>
    public void SetLicenceText(string licence) => LicenceText = licence;

    /// <inheritdoc/>
    public bool Prompt() => false;
}
