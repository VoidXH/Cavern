using System.IO;

namespace Cavern.Format.Container {
    /// <summary>
    /// Multimedia container reader base class.
    /// </summary>
    public abstract class ContainerReader {
        /// <summary>
        /// Content length in seconds.
        /// </summary>
        public double Duration { get; protected set; }

        /// <summary>
        /// File reader object.
        /// </summary>
        protected BinaryReader reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public ContainerReader(BinaryReader reader) => this.reader = reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        public ContainerReader(string path) => reader = new BinaryReader(File.OpenRead(path));
    }
}