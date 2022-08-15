using System.Collections.Generic;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains position/movement data for each channel contained in an object.
    /// </summary>
    public class ADMPackFormat : TaggedADMElement {
        /// <summary>
        /// Type of the contained tracks (channels, objects, etc.).
        /// </summary>
        public ADMPackType Type { get; set; }

        /// <summary>
        /// Positional data of the channels related to this object.
        /// </summary>
        public List<ADMChannelFormat> ChannelFormats { get; set; }
    }
}