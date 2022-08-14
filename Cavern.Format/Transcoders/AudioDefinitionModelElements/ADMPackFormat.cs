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
        /// Positional data of the channel.
        /// </summary>
        public ADMChannelFormat ChannelFormat { get; set; }
    }
}