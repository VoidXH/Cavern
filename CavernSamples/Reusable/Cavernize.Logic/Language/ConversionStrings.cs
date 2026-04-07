namespace Cavernize.Logic.Language;

/// <summary>
/// Strings for the messages of the conversion process.
/// </summary>
public class ConversionStrings {
    /// <summary>
    /// The root file was invalid. It must have an extension.
    /// </summary>
    public virtual string InvalidRootFile => "The root file was invalid. It must have an extension.";

    /// <summary>
    /// Convolution EQ file for the {0} channel was not found in this export ({1}).
    /// </summary>
    public virtual string ChannelFilterNotFound => "Convolution EQ file for the {0} channel was not found in this export ({1}).";
}
