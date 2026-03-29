using System.Windows.Controls;

using Cavern.Format.AI;

namespace Cavern.WPF.Controls;

/// <summary>
/// Holds a <see cref="ModelMetadata"/> and displays its values.
/// </summary>
public partial class NNMetadataDisplay : UserControl {
    /// <summary>
    /// The model of which the data is displayed.
    /// </summary>
    public INeuralNetwork Model {
        get => model;
        set {
            model = value;
            ModelMetadata metadata = model.Metadata;
            if (metadata.TrainingDataSize > 1_000_000_000) {
                trainingDataSize.Text = metadata.TrainingDataSize / 1_000_000_000 + " billion";
            } else if (metadata.TrainingDataSize > 1_000_000) {
                trainingDataSize.Text = metadata.TrainingDataSize / 1_000_000 + " million";
            } else if (metadata.TrainingDataSize > 1_000) {
                trainingDataSize.Text = metadata.TrainingDataSize / 1_000 + "k";
            } else {
                trainingDataSize.Text = metadata.TrainingDataSize.ToString();
            }
            testDataRatio.Text = metadata.TestDataRatio.ToString("0.#%");
            trainingLoss.Text = metadata.TrainingLoss.ToString("0.####");
            validationLoss.Text = metadata.ValidationLoss.ToString("0.####");
            inputParameters.Text = metadata.InputDimension.ToString();
            outputParameters.Text = metadata.OutputDimension.ToString();
        }
    }
    INeuralNetwork model;

    /// <summary>
    /// Initialize the <see cref="ModelMetadata"/> display.
    /// </summary>
    public NNMetadataDisplay() => InitializeComponent();
}
