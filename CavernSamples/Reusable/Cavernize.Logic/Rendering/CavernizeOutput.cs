using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Container;

using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;

namespace Cavernize.Logic.Rendering {
    /// <summary>
    /// Create output files for Cavernize renders.
    /// </summary>
    public static class CavernizeOutput {
        /// <summary>
        /// Create the output file(s) for regular conversions, and write the header(s).
        /// </summary>
        public static AudioWriter CreateRenderOutput(ICavernizeApp app, string path, ConversionEnvironment environment, CavernizeTrack target, Codec codec, BitDepth bits) {
            string exportFormat = path[^4..].ToLowerInvariant();
            bool mkvTarget = exportFormat.Equals(".mkv");
            string exportName = mkvTarget || exportFormat.IsNative() ?
                path[..^4] + waveExtension :
                path;
            int channelCount = app.RenderTarget.OutputChannels;
            AudioWriter writer;
            if (mkvTarget && target.Container == Container.Matroska && (codec == Codec.PCM_LE || codec == Codec.PCM_Float)) {
                int blockSize = GetBlockSize(app.RenderTarget, environment);
                writer = new AudioWriterIntoContainer(path, target.GetVideoTracks(), codec, blockSize, channelCount, target.Length, target.SampleRate, bits) {
                    NewTrackName = $"Cavern {app.RenderTarget.Name} render"
                };
            } else if (exportFormat.Equals(waveExtension) && !app.RenderingSettings.WAVChannelSkip) {
                writer = new RIFFWaveWriter(exportName, app.RenderTarget.Channels[..channelCount], target.Length, environment.Listener.SampleRate, bits);
            } else {
                writer = AudioWriter.Create(exportName, channelCount, target.Length, environment.Listener.SampleRate, bits);
            }
            if (writer == null) {
                return null;
            }
            writer.WriteHeader();
            return writer;
        }

        /// <summary>
        /// Get the write cache block size depending on active settings.
        /// </summary>
        public static int GetBlockSize(RenderTarget target, ConversionEnvironment environment) {
            int updateRate = environment.Listener.UpdateRate;
            int blockSize = defaultWriteCacheLength;
            if (blockSize < updateRate) {
                blockSize = updateRate;
            } else if (blockSize % updateRate != 0) {
                // Cache handling is written to only handle when its size is divisible with the update rate - it's faster this way
                blockSize += updateRate - blockSize % updateRate;
            }
            blockSize *= target.OutputChannels;
            return blockSize;
        }

        /// <summary>
        /// RIFF Wave file extension.
        /// </summary>
        const string waveExtension = ".wav";

        /// <summary>
        /// Default number of samples written per channel.
        /// </summary>
        const int defaultWriteCacheLength = 16384;
    }
}
