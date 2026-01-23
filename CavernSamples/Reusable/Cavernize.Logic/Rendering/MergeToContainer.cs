using Cavern.Format.Common;

using Cavernize.Logic.Models;
using VoidX.WPF.FFmpeg;

namespace Cavernize.Logic.Rendering;

/// <summary>
/// When a WAV result is available, use <see cref="FFmpeg"/> to merge it into a container format.
/// This runs in cases where an unsupported codec is set for export with a .mkv path.
/// </summary>
public sealed class MergeToContainer {
    /// <summary>
    /// Contains the setup for performing the merge with <see cref="FFmpeg"/>.
    /// </summary>
    readonly FFmpegConfigurator args = new();

    /// <summary>
    /// The Cavern-rendered audio track to merge with the source video and subtitle tracks.
    /// </summary>
    readonly string render;

    /// <summary>
    /// When a WAV result is available, use <see cref="FFmpeg"/> to merge it into a container format.
    /// </summary>
    public MergeToContainer(string sourceFile, string render, string outputCodec) {
        this.render = render;
        args.AddInputFile(sourceFile);
        args.AddInputFile(render);
        args.AddMappingIfExists(0, FFmpegStream.Video);
        args.AddMapping(1, FFmpegStream.Audio);
        args.AddMappingIfExists(0, FFmpegStream.Subtitle);
        args.SetCodec(FFmpegStream.Video, "copy");
        args.SetCodec(FFmpegStream.Audio, outputCodec);
        args.SetOverwrite(true);
    }

    /// <summary>
    /// Get file browser filters based on what output containers are supported for a given input and output codec.
    /// </summary>
    public static string GetPossibleContainers(CavernizeTrack input, Codec output, FFmpeg ffmpeg) {
        const string matroskaOnly = "Matroska|*.mkv";
        string native = output.GetSaveDialogFilter();
        if (output.IsEnvironmental()) {
            return output.GetSaveDialogFilter() ?? throw new NotImplementedException();
        } else if (output == Codec.PCM_Float || output == Codec.PCM_LE) {
            return ffmpeg.Found || input.Container == Container.Matroska ? $"{matroskaOnly}|{native}" : native;
        } else if (ffmpeg.Found) {
            return native == null ? matroskaOnly : $"{matroskaOnly}|{native}";
        } else {
            return matroskaOnly;
        }
    }

    /// <summary>
    /// Disable FFmpeg's internal mixer and any channel map export for supporting the maximum number of channels for each codec.
    /// </summary>
    public void Allow8PlusChannels() => args.Allow8PlusChannels();

    /// <summary>
    /// Remove some conditions under which FFmpeg would refuse to process the file.
    /// </summary>
    public void MakeSafe(string fileName) {
        if (fileName[fileName.IndexOf('.')..].IsNative()) {
            args.RemoveMapping(FFmpegStream.Video);
            args.RemoveMapping(FFmpegStream.Subtitle);
        }
    }

    /// <summary>
    /// Name of the <see cref="render"/>ed audio track merged to the output.
    /// </summary>
    public void SetTrackName(string name) => args.SetMetadata(FFmpegStream.Audio, 0, "title", name);

    /// <summary>
    /// Merge the video/subtitle streams of the source file with the <see cref="render"/>, and if successful, delete the render.
    /// </summary>
    /// <returns>The merge was successful</returns>
    public bool Merge(FFmpeg ffmpeg, string fileName) {
        args.OutputFile = fileName;
        if (ffmpeg.Launch(args.ToString()) && File.Exists(fileName)) {
            File.Delete(render);
            return true;
        }
        return false;
    }
}
