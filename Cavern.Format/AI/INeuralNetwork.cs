namespace Cavern.Format.AI {
    /// <summary>
    /// Layer definition for (deep) neural networks trained for Cavern operations.
    /// </summary>
    public interface INeuralNetwork {
        /// <summary>
        /// Additional information regarding the model.
        /// </summary>
        public ModelMetadata Metadata { get; }

        /// <summary>
        /// Export this neural network to a given <paramref name="path"/>.
        /// </summary>
        void Save(string path);

        /// <summary>
        /// Load a previously trained network of the derived type.
        /// </summary>
        INeuralNetwork Load(string path);
    }
}
