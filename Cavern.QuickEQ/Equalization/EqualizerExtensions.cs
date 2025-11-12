using System;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>
    /// Helper functions for <see cref="Equalizer"/>s.
    /// </summary>
    public static class EqualizerExtensions {
        /// <summary>
        /// Check if a <paramref name="measurement"/> is likeliy a measurement, based on its channel and spectra.
        /// </summary>
        public static bool IsLFE(this Equalizer measurement, int channel, int channels) => channels == Listener.Channels.Length ?
            Listener.Channels[channel].LFE :
            ((channel == 3 && channels >= 6) || Math.Abs(measurement[80] - measurement[8000]) > 50);
    }
}
