using Cavern.Format;
using Cavern.Format.Common;

using CavernizeGUI.Elements;

using Track = CavernizeGUI.Elements.Track;

namespace CavernizeGUI.CommandLine.HiddenCommands {
    /// <summary>
    /// A hacked version of <see cref="AudioFile"/> that loads the <see cref="overrider"/> stream.
    /// </summary>
    /// <param name="path">Path of the original E-AC-3 file</param>
    /// <param name="overrider">Stream to override the PCM data with - only applies to the source PCM data,
    /// not the JOC-decoded objects</param>
    public class OverrideBedFile(string path, AudioReader overrider) : AudioFile(path) {
        /// <summary>
        /// Reloads the tracklist to be able to start reading from the beginning.
        /// </summary>
        /// <remarks>Only supports codecs that can be overridden.</remarks>
        public override void Reset() {
            Dispose();
            tracks.Clear();
            switch (Path[^3..]) {
                case "ac3":
                case "eac3":
                case "ec3":
                    tracks.Add(new Track(new OverrideBedReader(Path, overrider), Codec.EnhancedAC3, 0));
                    break;
                default:
                    throw new CommandException("This command only supports Enhanced AC-3 files.");
            }
        }
    }
}