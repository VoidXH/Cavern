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
        /// The model's misaccuracy on test data.
        /// </summary>
        public float Loss { get; set; }

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
            writer.Write(Loss);
            writer.Write(InputDimension);
            writer.Write(OutputDimension);
        }

        /// <summary>
        /// Import the metadata from this <paramref name="path"/>.
        /// </summary>
        public void Load(string path) {
            using BinaryReader reader = new BinaryReader(File.OpenRead(path));
            TrainingDataSize = reader.ReadInt32();
            Loss = reader.ReadSingle();
            InputDimension = reader.ReadInt32();
            OutputDimension = reader.ReadInt32();
        }
    }
}
