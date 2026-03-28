namespace Cavern.Format.AI {
    /// <summary>
    /// Defines the <see cref="Input"/> and <see cref="Output"/> count of a model through an example training data.
    /// </summary>
    public interface ITrainingData {
        /// <summary>
        /// Each input value for a single training data.
        /// </summary>
        float[] Input { get; }

        /// <summary>
        /// Each output value for a single training data.
        /// </summary>
        float[] Output { get; }
    }
}
