using System.Collections.Generic;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains a group of objects of an ADM program.
    /// </summary>
    public class ADMContent : TaggedADMElement {
        /// <summary>
        /// Audio objects that are part of this content.
        /// </summary>
        public List<ADMObject> Objects { get; set; }
    }
}