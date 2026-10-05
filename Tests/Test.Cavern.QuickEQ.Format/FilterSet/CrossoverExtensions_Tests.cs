using Cavern.Format.FilterSet;
using Cavern.QuickEQ.Crossover;

using FSet = Cavern.Format.FilterSet.FilterSet;

namespace Test.Cavern.QuickEQ.Format.FilterSet {
    /// <summary>
    /// Tests the <see cref="CrossoverAnalyzerExtensions"/> and <see cref="CrossoverDescriptionExtensions"/> classes.
    /// </summary>
    [TestClass]
    public class CrossoverExtensions_Tests {
        /// <summary>
        /// Tests if <see cref="CrossoverDescriptionExtensions.AddToFilterSet"/> populates the crossover frequencies of the channels mixed from and
        /// leaves the mixing targets to null.
        /// </summary>
        [TestMethod]
        public void AddToFilterSet() {
            const int sampleRate = 48000;
            FSet filterSet = FSet.Create(FilterSetTarget.Generic, 6, sampleRate);
            CrossoverDescription description = new((false, 80f), (false, 80f), (false, 80f), (true, 0f), (false, 80f), (false, 80f));
            description.AddToFilterSet(filterSet);

            for (int i = 0; i < filterSet.Channels.Length; i++) {
                if (description.Mixing[i].mixHere) {
                    Assert.IsNull(filterSet.Channels[i].crossoverFrequency, $"Channel {i} (mixed to) should have no crossover frequency.");
                } else {
                    Assert.AreEqual(description.Mixing[i].crossoverFreq, filterSet.Channels[i].crossoverFrequency.Value,
                        $"Channel {i} should have a crossover frequency of {description.Mixing[i].crossoverFreq} Hz.");
                }
            }
        }
    }
}
