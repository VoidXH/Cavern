using System;

namespace Cavern.Format.AI {
    /// <summary>
    /// Framework for AI training.
    /// </summary>
    public abstract class Trainer {
        /// <summary>
        /// Additional information regarding the model.
        /// </summary>
        public ModelMetadata Metadata { get; } = new ModelMetadata();

        /// <summary>
        /// Feedback about where the <see cref="Train(int, int)"/> function is in completion.
        /// </summary>
        public float Progress { get; protected set; }

        /// <summary>
        /// Converted training data input values for easy memory access.
        /// </summary>
        protected float[] FlatInputs { get; }

        /// <summary>
        /// Converted training data output values for easy memory access.
        /// </summary>
        protected float[] FlatOutputs { get; }

        /// <summary>
        /// Parse and flatten the training <paramref name="data"/> to work with it faster.
        /// </summary>
        public Trainer(ITrainingData[] data) {
            Metadata.TrainingDataSize = data.Length;
            Metadata.InputDimension = data[0].Input.Length;
            Metadata.OutputDimension = data[0].Output.Length;
            FlatInputs = new float[data.Length * Metadata.InputDimension];
            FlatOutputs = new float[data.Length * Metadata.OutputDimension];
            for (int i = 0; i < data.Length; i++) {
                Array.Copy(data[i].Input, 0, FlatInputs, i * Metadata.InputDimension, Metadata.InputDimension);
            }
            for (int i = 0; i < data.Length; i++) {
                Array.Copy(data[i].Output, 0, FlatOutputs, i * Metadata.OutputDimension, Metadata.OutputDimension);
            }
        }
    }
}
