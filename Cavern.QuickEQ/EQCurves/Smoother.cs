using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Utilities;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// An EQ curve that smooths out inter-channel differences while keeping the system's sound character.
    /// </summary>
    public class Smoother : Custom {
        /// <summary>
        /// Create a custom EQ curve from a source stored as an Equalier.
        /// </summary>
        public Smoother(float[][] sourceFrequencyResponses, int sampleRate) : base(MakeCurve(sourceFrequencyResponses, sampleRate)) {}

        /// <summary>
        /// Create a target EQ from raw frequency responses, by averaging and smoothing the channels.
        /// </summary>
        static Equalizer MakeCurve(float[][] sourceFrequencyResponses, int sampleRate) {
            float[] target = new float[sourceFrequencyResponses[0].Length];
            for (int i = 0; i < sourceFrequencyResponses.Length; ++i) {
                for (int sample = 0; sample < sourceFrequencyResponses[i].Length; ++sample) {
                    target[sample] += sourceFrequencyResponses[i][sample];
                }
            }
            float gain = 1 / (float)sourceFrequencyResponses.Length;
            for (int sample = 0; sample < target.Length; ++sample) {
                target[sample] *= gain;
            }

            const float startFreq = 20,
                endFreq = 20000,
                smoothness = .75f;
            const int resultSize = 1000;
            target = GraphUtils.ConvertToGraph(target, startFreq, endFreq, sampleRate, resultSize);
            target = GraphUtils.SmoothGraph(target, startFreq, endFreq, smoothness);
            GraphUtils.ConvertToDecibels(target);
            GraphUtils.Normalize(target);
            return EQGenerator.FromGraph(target, startFreq, endFreq);
        }
    }
}