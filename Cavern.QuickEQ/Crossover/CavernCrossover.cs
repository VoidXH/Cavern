using System.Collections.Generic;
using System.Globalization;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// A FIR brickwall crossover, first introduced in Cavern.
    /// </summary>
    internal class CavernCrossover : BasicCrossover {
        /// <summary>
        /// Creates a FIR brickwall crossover, first introduced in Cavern.
        /// </summary>
        /// <param name="frequencies">Crossover frequencies for each channel, only values over 0 mean crossovered channels</param>
        /// <param name="subs">Channels to route bass to</param>
        public CavernCrossover(float[] frequencies, bool[] subs) : base(frequencies, subs) { }

        /// <summary>
        /// Add the filter's interpretation of highpass to the previously selected channel in a WIP configuration file.
        /// </summary>
        public override void AddHighpass(List<string> wipConfig, float frequency) {
            float offsetFreq = frequency * .967741875f; // Removes crossover notch caused by FIR resolution
            wipConfig.Add($"GraphicEQ: {(offsetFreq - 1).ToString(CultureInfo.InvariantCulture)} -48;" +
                $" {offsetFreq.ToString(CultureInfo.InvariantCulture)} 0");
        }

        /// <summary>
        /// Add the filter's interpretation of lowpass to the previously selected channel in a WIP configuration file.
        /// </summary>
        /// <remarks>Don't forget to call AddExtraOperations, this is generally the best place for it.</remarks>
        public override void AddLowpass(List<string> wipConfig, float frequency) {
            float offsetFreq = frequency * 1.032258f; // Removes crossover notch caused by FIR resolution
            wipConfig.Add($"GraphicEQ: {offsetFreq.ToString(CultureInfo.InvariantCulture)} 0;" +
                $" {(offsetFreq + 1).ToString(CultureInfo.InvariantCulture)} -48");
            AddExtraOperations(wipConfig);
        }
    }
}