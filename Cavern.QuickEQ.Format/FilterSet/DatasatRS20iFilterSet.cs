using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// 31-band graphic equalizer for Datasat RS20i processors.
    /// Channel gains are set with 0.5 dB accuracy, and bands are set between -6 and 6 dB with 0.2 dB accuracy.
    /// </summary>
    public class DatasatRS20iFilterSet : Multiband31FilterSet {
        /// <inheritdoc/>
        public override double MinGain => -6;

        /// <inheritdoc/>
        public override double MaxGain => 6;

        /// <inheritdoc/>
        public override double GainPrecision => .5;

        /// <summary>
        /// IIR filter set for Datasat RS20i processors with a given number of channels.
        /// </summary>
        public DatasatRS20iFilterSet(int channels, int sampleRate) : this(channels, sampleRate, true) { }

        /// <summary>
        /// IIR filter set for Datasat RS20i processors with a given number of channels.
        /// </summary>
        public DatasatRS20iFilterSet(ReferenceChannel[] channels, int sampleRate) : this(channels, sampleRate, true) { }

        /// <summary>
        /// IIR filter set for Datasat RS20i processors with a given number of channels.
        /// </summary>
        /// <param name="channels">Number of channels in the target system</param>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="roundedBands">Round filter center frequencies to the nearest whole number</param>
        public DatasatRS20iFilterSet(int channels, int sampleRate, bool roundedBands) : base(channels, sampleRate, roundedBands) { }

        /// <summary>
        /// IIR filter set for Datasat RS20i processors with a given set of channels.
        /// </summary>
        /// <param name="channels">Channels in the target system</param>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="roundedBands">Round filter center frequencies to the nearest whole number</param>
        public DatasatRS20iFilterSet(ReferenceChannel[] channels, int sampleRate, bool roundedBands) : base(channels, sampleRate, roundedBands) { }

        /// <inheritdoc/>
        public override void Export(string path) => File.WriteAllText(path, Export());

        /// <inheritdoc/>
        public override string Export() => Export(true);

        /// <inheritdoc/>
        protected override string Export(bool gainOnly) {
            IIRChannelData[] channels = Channels.Cast<IIRChannelData>().ToArray();
            for (int i = 0; i < channels.Length; i++) {
                BiquadFilter[] filters = channels[i].filters;
                for (int j = 0; j < filters.Length; j++) {
                    filters[j].Gain = QMath.RoundBy(filters[j].Gain, .2);
                }
            }
            return base.Export(gainOnly);
        }

    }
}
