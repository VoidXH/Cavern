using System.IO;

using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace CavernPipeServer {
    /// <summary>
    /// Reads CavernPipe control messages.
    /// </summary>
    public class CavernPipeProtocol {
        /// <summary>
        /// The PCM format in which the connected client expects the data.
        /// </summary>
        public BitDepth OutputFormat { get; }

        /// <summary>
        /// Calculated from byte 2 (number of frames to always render before sending a reply), this is the number of bytes in those frames.
        /// If this many bytes are not available, the client must wait for data.
        /// </summary>
        public int MandatoryBytesToSend { get; }

        /// <summary>
        /// Number of output channels of the client. If Cavern renders less according to user settings, additional channels are filled with silence.
        /// If Cavern renders more, excess channels will be cut off and a warning shall be shown.
        /// </summary>
        public int OutputChannels { get; }

        /// <summary>
        /// Number of samples expected in a reply PCM stream.
        /// </summary>
        public int UpdateRate { get; }

        /// <summary>
        /// Reads CavernPipe control messages.
        /// </summary>
        public CavernPipeProtocol(Stream source) {
            OutputFormat = (BitDepth)source.ReadByte();
            int mandatoryFrames = source.ReadByte();
            OutputChannels = source.ReadUInt16();
            UpdateRate = source.ReadInt32();
            if (UpdateRate <= 0) {
                throw new SyncException(); // Extension point for later
            }
            MandatoryBytesToSend = mandatoryFrames * OutputChannels * UpdateRate * ((int)OutputFormat / 8);
        }
    }
}