using System.IO;

using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Contains how many consecutive samples have a given duration. This is used for seeking.
    /// </summary>
    internal class TimeToSampleBox : Box {
        /// <summary>
        /// Contains how many consecutive samples have a given duration. This is used for seeking.
        /// </summary>
        public readonly (uint sampleCount, uint duration)[] durations;

        public TimeToSampleBox(uint length, Stream reader) : base(length, timeToSampleBox, reader) {
            reader.Position += 4; // Version byte and zero flags
            durations = new (uint, uint)[reader.ReadUInt32BE()];
            for (int i = 0; i < durations.Length; i++) {
                durations[i] = (reader.ReadUInt32BE(), reader.ReadUInt32BE());
            }
        }
    }
}