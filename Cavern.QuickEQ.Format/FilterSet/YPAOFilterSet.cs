using System.IO;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Filter set limited to 1/3 octave band choices for some versions of YPAO.
    /// </summary>
    public class YPAOFilterSet : IIRFilterSet {
        /// <inheritdoc/>
        public override int Bands => 7;

        /// <inheritdoc/>
        public override double MaxGain => 6;

        /// <inheritdoc/>
        public override double MinGain => -6;

        /// <inheritdoc/>
        public override double GainPrecision => .5f;

        /// <summary>
        /// Filter set limited to 1/3 octave band choices for some versions of YPAO.
        /// </summary>
        public YPAOFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Filter set limited to 1/3 octave band choices for some versions of YPAO.
        /// </summary>
        public YPAOFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        protected override string Export(bool gainOnly) {
            for (int i = 0; i < Channels.Length; i++) {
                BiquadFilter[] filters = ((IIRChannelData)Channels[i]).filters;
                for (int j = 0; j < filters.Length; j++) {
                    filters[j].Reset(frequencies.Nearest((float)filters[j].CenterFreq), qFactors.Nearest((float)filters[j].Q),
                        filters[j].Gain);
                }
            }
            return base.Export(gainOnly);
        }

        /// <inheritdoc/>
        public override void Export(string path) => File.WriteAllText(path, Export(false));

        /// <summary>
        /// All the possible bands that can be selected for YPAO. These are 1/3 octaves apart.
        /// </summary>
        static readonly float[] frequencies = {
            15.6f, 19.7f, 24.8f, 31.3f, 39.4f, 49.6f, 62.5f, 78.7f, 99.2f, 125.0f, 157.5f, 198.4f, 250, 315, 396.9f, 500, 630, 793.7f,
            1000, 1260, 1590, 2000, 2520, 3170, 4000, 5040, 6350, 8000, 10100, 12700, 16000
        };

        /// <summary>
        /// All the possible Q-factors that can be selected for YPAO.
        /// </summary>
        static readonly float[] qFactors = { 0.5f, 0.63f, 0.794f, 1f, 1.26f, 1.587f, 2f, 2.520f, 3.175f, 4f, 5.04f, 6.350f, 8f, 10.08f };
    }
}