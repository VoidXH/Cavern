using System;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Extension functions for <see cref="ConfigurationFileType"/>.
    /// </summary>
    public static class ConfigurationFileTypeExtensions {
        /// <summary>
        /// Convert the <paramref name="type"/> device to its name, and return null when the device is not available for single-measurement
        /// export, allowing for easier filtering of targets.
        /// </summary>
        public static string GetName(this ConfigurationFileType type) => type switch {
            ConfigurationFileType.CavernFilterStudio => "Cavern Filter Studio",
            ConfigurationFileType.ConvolutionBoxFormat => "Convolution Box Format",
            ConfigurationFileType.EqualizerAPO => "Equalizer APO",
            _ => throw new NotSupportedException()
        };
    }
}
