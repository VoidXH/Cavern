using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Container.MXF;

namespace Cavern.Format.Container {
    /// <summary>
    /// Reads a Material eXchange Format container's data structure.
    /// </summary>
    public class MXFReader : ContainerReader {
        /// <inheritdoc/>
        public override Common.Container Type => Common.Container.MXF;

        /// <summary>
        /// The pack containing the list of contained essences (track types).
        /// </summary>
        PackRegistry header;

        /// <summary>
        /// Reads a Material eXchange Format container's data structure.
        /// </summary>
        public MXFReader(Stream reader) : base(reader) => ReadSkeleton();

        /// <summary>
        /// Reads a Material eXchange Format container's data structure.
        /// </summary>
        public MXFReader(string path) : base(path) => ReadSkeleton();

        /// <inheritdoc/>
        public override bool IsNextBlockAvailable(int track) {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public override byte[] ReadNextBlock(int track) {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public override double Seek(double position) {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Read the metadata and basic block structure of the file.
        /// </summary>
        void ReadSkeleton() {
            while (reader.Position < reader.Length) {
                KeyLengthValueSMPTE block = KeyLengthValueSMPTE.Parse(reader);
                block.SeekToNext(reader);
                if (block is PackRegistry pack && pack.IsHeader) {
                    header = pack;
                    break;
                }
            }

            Tracks = new Track[header?.Length ?? 0];
            for (int i = 0; i < Tracks.Length; i++) {
                Tracks[i] = new Track(this, i);
            }
        }
    }
}