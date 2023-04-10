using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Container.Matroska;

namespace Cavern.Format.Container {
    /// <summary>
    /// Writes source <see cref="Track"/>s to a Matroska file.
    /// </summary>
    public class MatroskaWriter : ContainerWriter {
        /// <summary>
        /// Writes source <paramref name="tracks"/> to a Matroska file.
        /// </summary>
        public MatroskaWriter(Stream writer, Track[] tracks) : base(writer, tracks) { }

        /// <summary>
        /// Writes source <paramref name="tracks"/> to a Matroska file.
        /// </summary>
        public MatroskaWriter(string path, Track[] tracks) : base(path, tracks) { }

        /// <summary>
        /// Write the metadata that is present before the coded content.
        /// </summary>
        public override void WriteHeader() {
            MatroskaTreeWriter tree = new MatroskaTreeWriter(writer);
            tree.OpenSequence(MatroskaTree.EBML, 1);
            tree.Write(MatroskaTree.EBML_Version, (byte)1);
            tree.Write(MatroskaTree.EBML_ReadVersion, (byte)1);
            tree.Write(MatroskaTree.EBML_MaxIDLength, (byte)4);
            tree.Write(MatroskaTree.EBML_MaxSizeLength, (byte)8);
            tree.Write(MatroskaTree.EBML_DocType, "matroska");
            tree.Write(MatroskaTree.EBML_DocTypeVersion, (byte)1);
            tree.Write(MatroskaTree.EBML_DocTypeReadVersion, (byte)1);
            tree.CloseSequence();
        }
    }
}