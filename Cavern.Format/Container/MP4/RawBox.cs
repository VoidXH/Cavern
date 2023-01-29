using System.IO;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Raw data of the tracks in an MP4 container.
    /// </summary>
    internal class RawBox : Box {
        /// <summary>
        /// Raw data of the tracks in an MP4 container.
        /// </summary>
        public RawBox(uint length, Stream reader) : base(length, rawBox, reader) {
        }
    }
}