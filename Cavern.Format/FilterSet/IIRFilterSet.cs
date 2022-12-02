using System;

using Cavern.Filters;
using Cavern.Remapping;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Room correction filter data with infinite impulse response (biquad) filter sets for each channel.
    /// </summary>
    public abstract class IIRFilterSet : FilterSet {
        /// <summary>
        /// Applied filter sets for each channel in the configuration file.
        /// </summary>
        protected BiquadFilter[][] Filters { get; private set; }

        /// <summary>
        /// Room channel layout.
        /// </summary>
        protected ReferenceChannel[] Matrix { get; private set; }

        /// <summary>
        /// Read a room correction with IIR filter sets for each channel from a file.
        /// </summary>
        public IIRFilterSet(string path) {
            Matrix = ReadFile(path);
            Filters = new BiquadFilter[Matrix.Length][];
        }

        /// <summary>
        /// Construct a room correction with IIR filter sets for each channel for a room with the target number of channels.
        /// </summary>
        public IIRFilterSet(int channels) {
            Filters = new BiquadFilter[channels][];
            Matrix = ChannelPrototype.GetStandardMatrix(channels);
        }

        /// <summary>
        /// Construct a room correction with IIR filter sets for each channel for a room with the target reference channels.
        /// </summary>
        public IIRFilterSet(ReferenceChannel[] channels) {
            Filters = new BiquadFilter[channels.Length][];
            Matrix = channels;
        }

        /// <summary>
        /// Apply a filter set on the target system's selected channel.
        /// </summary>
        public void SetFilters(int channel, BiquadFilter[] filterSet) => Filters[channel] = filterSet;

        /// <summary>
        /// Apply a filter set on the target system's selected channel.
        /// </summary>
        public void SetFilters(ReferenceChannel channel, BiquadFilter[] filterSet) {
            for (int i = 0; i < Matrix.Length; ++i) {
                if (Matrix[i] == channel) {
                    Filters[i] = filterSet;
                    return;
                }
            }
        }

        /// <summary>
        /// When overridden, the filter set supports file import through this function.
        /// </summary>
        /// <returns>Room layout</returns>
        protected virtual ReferenceChannel[] ReadFile(string path) => throw new NotImplementedException();
    }
}