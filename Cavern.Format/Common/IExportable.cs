namespace Cavern.Format.Common {
    /// <summary>
    /// Interface for a format that can be exported to a file.
    /// </summary>
    public interface IExportable {
        /// <summary>
        /// Extension of the main file created with <see cref="Export(string)"/>. If multiple files are created, this is the extension of
        /// the root file. This should be displayed on export dialogs.
        /// </summary>
        public string FileExtension { get; }

        /// <summary>
        /// Export this object to a target file.
        /// </summary>
        /// <param name="path">Location of the target file</param>
        public void Export(string path);
    }
}