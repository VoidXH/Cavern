using System.Collections.Generic;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains a single ADM object with multiple possible tracks.
    /// </summary>
    public class ADMObject : TaggedADMElement {
        /// <summary>
        /// Position/movement data for each contained channel.
        /// </summary>
        public ADMPackFormat PackFormat { get; set; }
    }
}