using System;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.QuickEQ.PolarityCorrections {
    /// <summary>
    /// Provides an algorithm for getting which channels have inverted polarities and a way to swap them.
    /// </summary>
    public abstract class PolarityCorrection {
        /// <summary>
        /// Apply the result of a correcting <paramref name="polarities"/> on any <paramref name="set"/> of signals.
        /// </summary>
        public static void Apply(MultichannelWaveform set, bool[] polarities) {
            for (int i = 0; i < polarities.Length; i++) {
                if (polarities[i]) {
                    WaveformUtils.Invert(set[i]);
                }
            }
        }

        /// <summary>
        /// Get which of the passed <paramref name="simulations"/> have inverted phase using a specific <paramref name="method"/>.
        /// </summary>
        public static bool[] GetInvertedChannels(PolarityCorrectionType method, MultichannelWaveform simulations) {
            if (method == PolarityCorrectionType.None) {
                return new bool[simulations.Channels];
            }

            PolarityCorrection corrector = method switch {
                PolarityCorrectionType.ConstructivityBased => new ConstructivityBasedPolarityCorrection(),
                PolarityCorrectionType.ImpulsePeakBased => new ImpulsePeakBasedPolarityCorrection(),
                _ => throw new NotImplementedException()
            };
            return corrector.GetInvertedChannels(simulations);
        }

        /// <summary>
        /// Get which of the passed <paramref name="simulations"/> have inverted phase.
        /// </summary>
        public abstract bool[] GetInvertedChannels(MultichannelWaveform simulations);

        /// <summary>
        /// Get which of the passed <paramref name="measurements"/> will have inverted phase when its <paramref name="filters"/> are applied.
        /// </summary>
        public bool[] GetInvertedChannels(MultichannelWaveform measurements, MultichannelWaveform filters) =>
            GetInvertedChannels(MultichannelConvolver.ConvolveSafe(measurements, filters));
    }
}