using System.IO;
using System.Text;

namespace Cavern.Format.Transcoders {
    /// <summary>
    /// An XML file with channel and object information.
    /// </summary>
    public class AudioDefinitionModel {
        /// <summary>
        /// Parses an XML file with channel and object information.
        /// </summary>
        public AudioDefinitionModel(Stream reader, int length) {
            byte[] data = new byte[length];
            reader.Read(data, 0, length);
            string raw = Encoding.UTF8.GetString(data);
        }
    }
}