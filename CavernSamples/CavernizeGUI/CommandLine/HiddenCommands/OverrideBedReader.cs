using Cavern.Format;
using Cavern.Format.Utilities;

namespace CavernizeGUI.CommandLine.HiddenCommands {
    /// <summary>
    /// Enhanced AC-3 file reader that reads the PCM data from another file.
    /// </summary>
    public class OverrideBedReader : EnhancedAC3Reader {
        /// <summary>
        /// Stream to override the PCM data with. Only applies to the source PCM data, not the JOC-decoded objects.
        /// </summary>
        readonly AudioReader overrider;

        /// <summary>
        /// Enhanced AC-3 file reader that reads the PCM data from another file.
        /// </summary>
        public OverrideBedReader(string path, AudioReader overrider) : base(path) => this.overrider = overrider;

        /// <summary>
        /// Read the file header.
        /// </summary>
        public override void ReadHeader() {
            decoder = new OverrideBedDecoder(BlockBuffer<byte>.Create(reader, 10 * 1024 * 1024), fileSize, overrider);
            ChannelCount = decoder.ChannelCount;
            Length = decoder.Length;
            SampleRate = decoder.SampleRate;
        }
    }
}