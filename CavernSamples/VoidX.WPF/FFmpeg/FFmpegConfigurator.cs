using System.Text;

namespace VoidX.WPF.FFmpeg;

/// <summary>
/// Set up FFmpeg arguments and build the command line params using <see cref="ToString"/>.
/// </summary>
public sealed class FFmpegConfigurator {
    /// <summary>
    /// Path to the resulting file.
    /// </summary>
    public string OutputFile { get; set; }

    /// <summary>
    /// Arguments preceding inputs and the inputs themselves.
    /// </summary>
    readonly List<FFmpegArgument> inputs = [];

    /// <summary>
    /// Arguments between the inputs and output.
    /// </summary>
    readonly List<FFmpegArgument> settings = [];

    /// <summary>
    /// Count of -i arguments in <see cref="inputs"/>.
    /// </summary>
    int inputFiles;

    /// <summary>
    /// Add an input file with the arguments that precede it (like time offset for fast search).
    /// </summary>
    public void AddInputFile(string path, params FFmpegArgument[] arguments) {
        if (arguments.Length != 0) {
            inputs.AddRange(arguments);
        }
        inputs.Add(new FFmpegArgument("-i", path));
        inputFiles++;
    }

    /// <summary>
    /// Select a substream category of an input file.
    /// </summary>
    public void AddMapping(int inputFile, FFmpegStream streamType) {
        CheckInputFile(inputFile);
        settings.Add(new("-map", $"{inputFile}:{(char)streamType}"));
    }

    /// <summary>
    /// Select a substream of an input file.
    /// </summary>
    public void AddMapping(int inputFile, FFmpegStream streamType, int streamIndex) {
        CheckInputFile(inputFile);
        settings.Add(new("-map", $"{inputFile}:{(char)streamType}:{streamIndex}"));
    }

    /// <summary>
    /// Select all substreams of an input file of a given type, if any exist.
    /// </summary>
    public void AddMappingIfExists(int inputFile, FFmpegStream streamType) {
        CheckInputFile(inputFile);
        settings.Add(new("-map", $"{inputFile}:{(char)streamType}?"));
    }

    /// <summary>
    /// Disable FFmpeg's internal mixer and any channel map export for supporting the maximum number of channels for each codec.
    /// </summary>
    public void Allow8PlusChannels() => settings.Add(new("-mapping_family", "255"));

    /// <summary>
    /// Set the codec for the output.
    /// </summary>
    public void SetCodec(FFmpegStream streamType, string codec) => settings.Add(new("-c:" + (char)streamType, codec));

    /// <summary>
    /// Set a metadata key-value pair for a specific output stream.
    /// </summary>
    public void SetMetadata(FFmpegStream streamType, int streamIndex, string key, string value) =>
        settings.Add(new($"-metadata:s:{(char)streamType}:{streamIndex}", $"{key}={value}"));

    /// <summary>
    /// Force overwriting or skipping files instead of asking it.
    /// </summary>
    public void SetOverwrite(bool overwrite) => settings.Add(new(overwrite ? "-y" : "-n", null));

    /// <summary>
    /// Build the command line argument list.
    /// </summary>
    public override string ToString() {
        if (string.IsNullOrEmpty(OutputFile)) {
            throw new InvalidOperationException("Output file must be set before converting to string.");
        }

        StringBuilder result = new();
        for (int i = 0, c = inputs.Count; i < c; i++) {
            inputs[i].ToString(result);
        }

        for (int i = 0, c = settings.Count; i < c; i++) {
            settings[i].ToString(result);
        }

        if (OutputFile.Contains(' ')) {
            result.Append('"').Append(OutputFile.Replace("\"", "\\\"")).Append('"');
        } else {
            result.Append(OutputFile);
        }
        return result.ToString();
    }

    /// <summary>
    /// Throw an exception if the given input file index is out of range.
    /// </summary>
    void CheckInputFile(int index) {
        if (index < 0 || index >= inputFiles) {
            throw new ArgumentOutOfRangeException(nameof(index), "Input file index is out of range.");
        }
    }
}