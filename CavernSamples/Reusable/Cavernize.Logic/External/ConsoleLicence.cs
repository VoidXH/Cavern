using Cavern.Utilities;

namespace Cavernize.Logic.External;

/// <summary>
/// Console licence prompt for external tools used by Cavernize.
/// </summary>
public sealed class ConsoleLicence : ILicence {
    string description;
    string licence;

    /// <inheritdoc/>
    public void SetDescription(string description) => this.description = description;

    /// <inheritdoc/>
    public void SetLicenceText(string licence) => this.licence = licence;

    /// <inheritdoc/>
    public bool Prompt() {
        if (!string.IsNullOrWhiteSpace(description)) {
            Console.WriteLine(description);
            Console.WriteLine();
        }
        if (!string.IsNullOrWhiteSpace(licence)) {
            Console.WriteLine(licence);
            Console.WriteLine();
        }

        Console.Write("Accept licence? [y/N] ");
        string answer = Console.ReadLine();
        return answer != null && answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
    }
}
