using Cavernize.Logic.Models;

namespace Cavernize.Logic.CommandLine.HiddenCommands;

internal class FFmpegArgCommand : UnsafeCommand {
    /// <inheritdoc/>
    public override string Name => "--ffmpeg-arg";

    /// <inheritdoc/>
    public override int Parameters => 2;

    /// <inheritdoc/>
    public override string Help => "Add custom arguments to FFmpeg's calls when using it for merging to containers.";

    /// <inheritdoc/>
    public override void Execute(string[] args, int offset, ICavernizeApp app) => app.RenderingSettings.MergeArguments.Add(new(args[offset], args[offset + 1]));
}
