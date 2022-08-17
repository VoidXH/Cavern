namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// An ADM element with an ID and a name.
    /// </summary>
    public abstract class TaggedADMElement {
        /// <summary>
        /// Identifier of the element.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Name of the element.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Displays the ID and the name of the element.
        /// </summary>
        public override string ToString() => $"[{ID}] {Name}";
    }
}