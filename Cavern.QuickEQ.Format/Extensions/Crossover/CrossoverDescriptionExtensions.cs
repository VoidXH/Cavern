using Cavern.Channels;
using Cavern.Format.FilterSet;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Operations on <see cref="CrossoverDescription"/> that require Cavern.QuickEQ.Format classes.
    /// </summary>
    public static class CrossoverDescriptionExtensions {
        /// <summary>
        /// Export the crossover frequencies described by a <paramref name="description"/> to a <paramref name="filterSet"/>, setting each channel's
        /// <see cref="ChannelData.crossoverFrequency"/> to the frequency where that channel is crossed over at.
        /// </summary>
        /// <param name="description">The crossover settings to be exported</param>
        /// <param name="filterSet">The filter set to populate with the crossover frequencies</param>
        /// <remarks>Channels that are subwoofers or full range are left with a null crossover frequency.</remarks>
        public static void AddToFilterSet(this CrossoverDescription description, FilterSet filterSet) {
            if (filterSet.ChannelCount != description.Channels) {
                throw new ChannelCountMismatchException();
            }

            for (int i = 0; i < filterSet.Channels.Length; i++) {
                ChannelData channel = filterSet.Channels[i];
                if (i < description.Mixing.Length && !description.Mixing[i].mixHere) {
                    channel.crossoverFrequency = description.Mixing[i].crossoverFreq;
                } else {
                    channel.crossoverFrequency = null;
                }
            }
        }
    }
}
