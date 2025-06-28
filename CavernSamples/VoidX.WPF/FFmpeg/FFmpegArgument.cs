using System.Text;

namespace VoidX.WPF.FFmpeg;

/// <summary>
/// A single FFmpeg argument as a key-value pair.
/// </summary>
public readonly struct FFmpegArgument(string key, string value) {
    /// <summary>
    /// FFmpeg command line argument name.
    /// </summary>
    public readonly string key = key;

    /// <summary>
    /// Value to be passed for this argument.
    /// </summary>
    public readonly string value = value;

    /// <summary>
    /// Append this argument to a set of arguments under constructions.
    /// </summary>
    public void ToString(StringBuilder builder) {
        builder.Append(key);
        builder.Append(' ');
        if (string.IsNullOrEmpty(value)) {
            return;
        }

        if (value.Contains(' ')) {
            builder.Append('"').Append(value.Replace("\"", "\\\"")).Append('"');
        } else {
            builder.Append(value);
        }
        builder.Append(' ');
    }
}