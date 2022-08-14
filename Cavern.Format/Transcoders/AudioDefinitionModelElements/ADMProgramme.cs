using System.Collections.Generic;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Root element of the model's contained program.
    /// </summary>
    public class ADMProgramme : TaggedADMElement {
        /// <summary>
        /// Groups of contained objects.
        /// </summary>
        public List<ADMContent> Contents { get; set; }
    }
}
