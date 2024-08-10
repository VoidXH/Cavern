using Cavern.Channels;
using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet.BaseClasses {
    /// <summary>
    /// An <see cref="IIRFilterSet"/> where the selection of frequencies and Q factors are limited to a predetermined set.
    /// </summary>
    public abstract class LimitedIIRFilterSet : IIRFilterSet {
        /// <summary>
        /// The allowed frequency values in ascending order.
        /// </summary>
        protected abstract float[] Frequencies { get; }

        /// <summary>
        /// The allowed Q factor values in ascending order.
        /// </summary>
        protected abstract float[] QFactors { get; }

        /// <summary>
        /// An <see cref="IIRFilterSet"/> where the selection of frequencies and Q factors are limited to a predetermined set.
        /// </summary>
        protected LimitedIIRFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// An <see cref="IIRFilterSet"/> where the selection of frequencies and Q factors are limited to a predetermined set.
        /// </summary>
        protected LimitedIIRFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        protected override string Export(bool gainOnly) {
            for (int i = 0; i < Channels.Length; i++) {
                BiquadFilter[] filters = ((IIRChannelData)Channels[i]).filters;
                for (int j = 0; j < filters.Length; j++) {
                    filters[j].Reset(Frequencies.Nearest((float)filters[j].CenterFreq), QFactors.Nearest((float)filters[j].Q),
                        filters[j].Gain);
                }
            }
            return base.Export(gainOnly);
        }
    }
}