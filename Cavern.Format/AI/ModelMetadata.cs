using System.IO;

namespace Cavern.Format.AI {
    /// <summary>
    /// Extra information for a model trained with Cavern.
    /// </summary>
    public class ModelMetadata {
        /// <summary>
        /// Number of input data that was used for the actual training.
        /// </summary>
        public int TrainingDataSize { get; set; }

        /// <summary>
        /// What percentage of the training data was used for testing the model, which is not used for training, but only to calculate the loss.
        /// </summary>
        public float TestDataRatio { get; set; }

        /// <summary>
        /// Number of training iterations performed on the model.
        /// </summary>
        public int TrainingEpochs { get; set; }

        /// <summary>
        /// The model's misaccuracy on training data.
        /// </summary>
        public float TrainingLoss { get; set; }

        /// <summary>
        /// The model's misaccuracy on test data.
        /// </summary>
        public float ValidationLoss { get; set; }

        /// <summary>
        /// Number of input ports.
        /// </summary>
        public int InputDimension { get; set; }

        /// <summary>
        /// Number of output ports.
        /// </summary>
        public int OutputDimension { get; set; }

        /// <summary>
        /// Export the metadata to this <paramref name="path"/>.
        /// </summary>
        public void Save(string path) {
            using BinaryWriter writer = new BinaryWriter(File.OpenWrite(path));
            writer.Write(TrainingDataSize);
            writer.Write(TestDataRatio);
            writer.Write(TrainingEpochs);
            writer.Write(TrainingLoss);
            writer.Write(ValidationLoss);
            writer.Write(InputDimension);
            writer.Write(OutputDimension);
        }

        /// <summary>
        /// Import the metadata from this <paramref name="path"/>.
        /// </summary>
        public void Load(string path) {
            using BinaryReader reader = new BinaryReader(File.OpenRead(path));
            TrainingDataSize = reader.ReadInt32();
            TestDataRatio = reader.ReadSingle();
            TrainingEpochs = reader.ReadInt32();
            TrainingLoss = reader.ReadSingle();
            ValidationLoss = reader.ReadSingle();
            InputDimension = reader.ReadInt32();
            OutputDimension = reader.ReadInt32();
        }

        /// <summary>
        /// Import the metadata from an<paramref name="other"/> instance.
        /// </summary>
        public void Load(ModelMetadata other) {
            TrainingDataSize = other.TrainingDataSize;
            TestDataRatio = other.TestDataRatio;
            TrainingEpochs = other.TrainingEpochs;
            TrainingLoss = other.TrainingLoss;
            ValidationLoss = other.ValidationLoss;
            InputDimension = other.InputDimension;
            OutputDimension = other.OutputDimension;
        }
    }
}
