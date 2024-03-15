using Cavern.QuickEQ.Equalization;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// An EQ curve that smooths out inter-channel differences while keeping the system's sound character.
    /// </summary>
    public class Smoother : Custom {
        /// <summary>
        /// Create a custom EQ curve from a source stored as an Equalier.
        /// </summary>
        public Smoother(Equalizer[] sourceFrequencyResponses) : base(MakeCurve(sourceFrequencyResponses)) {}

        /// <summary>
        /// Create a target EQ from raw frequency responses, by averaging and smoothing the channels.
        /// </summary>
        static Equalizer MakeCurve(Equalizer[] sourceFrequencyResponses) {
            Equalizer target = EQGenerator.AverageRMS(sourceFrequencyResponses);
            target.Smooth(.75);
            target.Normalize(500, 10000);
            return target;
        }
    }
}