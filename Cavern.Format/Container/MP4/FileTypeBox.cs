using System.IO;
using System.Linq;

using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// General file information box.
    /// </summary>
    internal class FileTypeBox : Box {
        /// <summary>
        /// FourCC ID of the container.
        /// </summary>
        uint MajorBrand { get; }

        /// <summary>
        /// Minimum required decoder/specification version.
        /// </summary>
        uint MinorVersion { get; }

        /// <summary>
        /// FourCC IDs of the contained contents' formats.
        /// </summary>
        uint[] CompatibleBrands { get; }

        /// <summary>
        /// Parse a file type box.
        /// </summary>
        public FileTypeBox(uint length, Stream reader) : base(length, fileTypeBox, reader) {
            MajorBrand = reader.ReadUInt32BE();
            MinorVersion = reader.ReadUInt32BE();
            CompatibleBrands = new uint[(length - 8) >> 2];
            for (int i = 0; i < CompatibleBrands.Length; i++) {
                CompatibleBrands[i] = reader.ReadUInt32BE();
            }
        }

        /// <summary>
        /// Human-readable display of the general file type info.
        /// </summary>
        public override string ToString() {
            string result = $"File type: {MajorBrand.ToFourCC()} version {MinorVersion}";
            return CompatibleBrands.Length == 0 ? result :
                $"{result}, compatible brands: {string.Join(", ", CompatibleBrands.Select(x => x.ToFourCC()))}";
        }
    }
}