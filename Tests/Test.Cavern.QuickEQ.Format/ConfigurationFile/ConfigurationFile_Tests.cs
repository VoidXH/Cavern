using Cavern;
using Cavern.Format.ConfigurationFile;
using Cavern.Format.FilterSet;

using ConfigurationFileImpl = Cavern.Format.ConfigurationFile.ConfigurationFile;
using FilterSetImpl = Cavern.Format.FilterSet.FilterSet;

namespace Test.Cavern.QuickEQ.Format.ConfigurationFile;

/// <summary>
/// Tests the <see cref="ConfigFile"/> class.
/// </summary>
[TestClass]
public class ConfigurationFile_Tests {
    /// <summary>
    /// Tests if an <see cref="EqualizerFilterSet"/> can be parsed into a <see cref="ConfigurationFileImpl"/> without exceptions.
    /// </summary>
    [TestMethod]
    public void EqualizerFilterSetToConfigFile() {
        EqualizerFilterSet filterSet = (EqualizerFilterSet)FilterSetImpl.Create(FilterSetTarget.GenericEqualizer, 4, Listener.DefaultSampleRate);
        for (int i = 0; i < filterSet.ChannelCount; i++) {
            filterSet.SetupChannel(i, new());
        }
        ConfigurationFileImpl.Create(ConfigurationFileType.EqualizerAPO, string.Empty, filterSet);
    }
}
