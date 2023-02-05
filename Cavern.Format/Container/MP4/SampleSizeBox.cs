using System.IO;

using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Box of sizes for all samples.
    /// </summary>
    internal class SampleSizeBox : Box {
        /// <summary>
        /// Each sample has this size. If this value is 0, use the <see cref="sizes"/> array, because all samples have a different size.
        /// </summary>
        public readonly uint size;

        /// <summary>
        /// Size of each sample if there's no common <see cref="size"/>.
        /// </summary>
        public readonly uint[] sizes;

        /// <summary>
        /// Box of sizes for all samples.
        /// </summary>
        public SampleSizeBox(uint length, Stream reader) : base(length, sampleSizeBox, reader) {
            reader.Position += 4; // Version byte and zero flags
            size = reader.ReadUInt32BE();
            if (size == 0) {
                sizes = new uint[reader.ReadUInt32BE()];
                for (int i = 0; i < sizes.Length; i++) {
                    sizes[i] = reader.ReadUInt32BE();
                }
            }
        }
    }
}