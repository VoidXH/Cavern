using System.Collections.Generic;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Positional data of a channel/object.
    /// </summary>
    public class ADMChannelFormat : TaggedADMElement {
        /// <summary>
        /// Positional data for each timeslot.
        /// </summary>
        public List<ADMBlockFormat> Blocks { get; set; }
    }
}