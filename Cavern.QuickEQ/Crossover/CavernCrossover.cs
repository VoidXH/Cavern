using System.Collections.Generic;
using System.Globalization;

using Cavern.QuickEQ.Equalization;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// A FIR brickwall crossover, first introduced in Cavern.
    /// </summary>
    public class CavernCrossover : BasicCrossover {
        /// <summary>
        /// Creates a FIR brickwall crossover, first introduced in Cavern.
        /// </summary>
        /// <param name="frequencies">Crossover frequencies for each channel, only values over 0 mean crossovered channels</param>
        /// <param name="subs">Channels to route bass to</param>
        public CavernCrossover(float[] frequencies, bool[] subs) : base(frequencies, subs) { }

        /// <inheritdoc/>
        public override void AddHighpass(List<string> wipConfig, float frequency) {
            float offsetFreq = frequency * .967741875f; // Removes crossover notch caused by FIR resolution
            wipConfig.Add($"GraphicEQ: {(offsetFreq - 1).ToString(CultureInfo.InvariantCulture)} -48;" +
                $" {offsetFreq.ToString(CultureInfo.InvariantCulture)} 0");
        }

        /// <inheritdoc/>
        public override float[] GetHighpass(int sampleRate, float frequency, int length) => new Equalizer(new List<Band> {
            new Band(frequency * .967741875f, -48), // Removes crossover notch caused by FIR resolution
            new Band(frequency, 0)
        }, true).GetConvolution(sampleRate, length);

        /// <inheritdoc/>
        public override void AddLowpass(List<string> wipConfig, float frequency) {
            float offsetFreq = frequency * 1.032258f; // Removes crossover notch caused by FIR resolution
            wipConfig.Add($"GraphicEQ: {offsetFreq.ToString(CultureInfo.InvariantCulture)} 0;" +
                $" {(offsetFreq + 1).ToString(CultureInfo.InvariantCulture)} -48");
            AddExtraOperations(wipConfig);
        }

        /// <inheritdoc/>
        public override float[] GetLowpass(int sampleRate, float frequency, int length) => new Equalizer(new List<Band> {
            new Band(frequency, 0),
            new Band(frequency * 1.032258f, -48) // Removes crossover notch caused by FIR resolution
        }, true).GetConvolution(sampleRate, length);
    }
}