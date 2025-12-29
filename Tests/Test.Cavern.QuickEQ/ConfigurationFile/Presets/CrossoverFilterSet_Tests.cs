using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;
using Cavern.Format.ConfigurationFile.Presets;
using Cavern.QuickEQ.Crossover;

using Test.Cavern.QuickEQ.Utilities;

using ConfigFile = Cavern.Format.ConfigurationFile.ConfigurationFile;
using Xover = Cavern.QuickEQ.Crossover.Crossover;

namespace Test.Cavern.QuickEQ.ConfigurationFile.Presets {
    /// <summary>
    /// Tests the <see cref="CrossoverFilterSet"/> class.
    /// </summary>
    [TestClass]
    public class CrossoverFilterSet_Tests {
        /// <summary>
        /// Returns true if the LFE or non-LFE channels get filtered by a <see cref="WaldoFilter"/>.
        /// </summary>
        static bool HitsWaldo(ConfigFile config, CrossoverDescription crossover, bool checkLFE) {
            (string name, FilterGraphNode root)[] channels = config.InputChannels;
            for (int i = 0; i < channels.Length; i++) {
                if (crossover.Mixing[i].mixHere == checkLFE) {
                    HashSet<FilterGraphNode> map = FilterGraphNodeUtils.MapGraph([channels[i].root]);
                    if (map.Any(x => x.Filter is WaldoFilter)) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Preparation for adding filters using the standard API of <see cref="CrossoverFilterSet"/>, like <see cref="CrossoverFilterSet.OnCrossovered"/>.
        /// Creates the <see cref="CrossoverFilterSet"/> where only the filter has to be set.
        /// </summary>
        static CrossoverFilterSet FilterTestPre(Xover crossover) => new(string.Empty, crossover, Consts.sampleRate, Consts.convolutionLength);

        /// <summary>
        /// Test if the given filter is present and present only on the wanted channels.
        /// </summary>
        static void FilterTestPost(CrossoverFilterSet set, Xover crossover, bool onMains, bool onLFE) {
            CrossoverDescription mixing = crossover.Mixing;
            CavernFilterStudioConfigurationFile config = new(string.Empty, mixing.Channels);
            FilterGraphNode[] outputs = [.. config.InputChannels.Select(x => x.root.Children[0])];
            set.Add(config, 0);

            Assert.AreEqual(onMains, HitsWaldo(config, mixing, false), "Failed on mains.");
            Assert.AreEqual(onLFE, HitsWaldo(config, mixing, true), "Failed on LFE(s).");

            (string name, FilterGraphNode root)[] inputs = config.InputChannels;
            for (int i = 0; i < inputs.Length; i++) {
                inputs[i].root.AssertReachesOwnOutput();
                outputs[i].AssertReachesOwnInput();
            }
        }

        /// <summary>
        /// Tests if <see cref="CrossoverFilterSet.OnCrossovered"/> works as intended on the selected system.
        /// </summary>
        static void OnCrossovered(Xover crossover) {
            CrossoverFilterSet set = FilterTestPre(crossover);
            set.OnCrossovered = new WaldoFilter();
            FilterTestPost(set, crossover, true, false);
        }

        /// <summary>
        /// Tests if <see cref="CrossoverFilterSet.OnLFEInput"/> works as intended on the selected system.
        /// </summary>
        static void OnLFEInput(Xover crossover) {
            CrossoverFilterSet set = FilterTestPre(crossover);
            set.OnLFEInput = new WaldoFilter();
            FilterTestPost(set, crossover, false, true);
        }

        /// <summary>
        /// Tests if <see cref="CrossoverFilterSet.OnEntireBass"/> works as intended on the selected system.
        /// </summary>
        static void OnEntireBass(Xover crossover) {
            CrossoverFilterSet set = FilterTestPre(crossover);
            set.OnEntireBass = new WaldoFilter();
            FilterTestPost(set, crossover, true, true);
        }

        /// <summary>
        /// Tests if <see cref="CrossoverFilterSet.OnCrossovered"/> works as intended on a multi-subwoofer system (4.2).
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void OnCrossovered4_2() => OnCrossovered(Crossovers.Basic4_2);

        /// <summary>
        /// Tests if <see cref="CrossoverFilterSet.OnLFEInput"/> works as intended on a multi-subwoofer system (4.2).
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void OnLFEInput4_2() => OnLFEInput(Crossovers.Basic4_2);

        /// <summary>
        /// Tests if <see cref="CrossoverFilterSet.OnEntireBass"/> works as intended on a multi-subwoofer system (4.2).
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void OnEntireBass4_2() => OnEntireBass(Crossovers.Basic4_2);

        /// <summary>
        /// Tests if <see cref="CrossoverFilterSet.OnCrossovered"/> works as intended on a single subwoofer system (5.1).
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void OnCrossovered5_1() => OnCrossovered(Crossovers.Basic5_1);

        /// <summary>
        /// Tests if <see cref="CrossoverFilterSet.OnLFEInput"/> works as intended on a single subwoofer system (5.1).
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void OnLFEInput5_1() => OnLFEInput(Crossovers.Basic5_1);

        /// <summary>
        /// Tests if <see cref="CrossoverFilterSet.OnEntireBass"/> works as intended on a single subwoofer system (5.1).
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void OnEntireBass5_1() => OnEntireBass(Crossovers.Basic5_1);
    }
}
