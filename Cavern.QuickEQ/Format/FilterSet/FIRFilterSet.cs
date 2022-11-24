using System;

using Cavern.Remapping;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Room correction filter data with a finite impulse response (convolution) filter for each channel.
    /// </summary>
    public abstract class FIRFilterSet {
        /// <summary>
        /// Applied convolution filters for each channel in the configuration file.
        /// </summary>
        protected float[][] Filters { get; private set; }

        /// <summary>
        /// Room channel layout.
        /// </summary>
        protected ReferenceChannel[] Matrix { get; private set; }

        /// <summary>
        /// Read a room correction with a FIR filter for each channel from a file.
        /// </summary>
        public FIRFilterSet(string path) {
            Matrix = ReadFile(path);
            Filters = new float[Matrix.Length][];
        }

        /// <summary>
        /// Construct a room correction with a FIR filter for each channel for a room with the target number of channels.
        /// </summary>
        public FIRFilterSet(int channels) {
            Filters = new float[channels][];
            Matrix = ChannelPrototype.GetStandardMatrix(channels);
        }

        /// <summary>
        /// Construct a room correction with a FIR filter for each channel for a room with the target reference channels.
        /// </summary>
        public FIRFilterSet(ReferenceChannel[] channels) {
            Filters = new float[channels.Length][];
            Matrix = channels;
        }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public abstract void Export(string path);

        /// <summary>
        /// Apply a filter on the target system's selected channel.
        /// </summary>
        public void SetFilters(int channel, float[] filter) => Filters[channel] = filter;

        /// <summary>
        /// Apply a filter on the target system's selected channel.
        /// </summary>
        public void SetFilters(ReferenceChannel channel, float[] filter) {
            for (int i = 0; i < Matrix.Length; ++i) {
                if (Matrix[i] == channel) {
                    Filters[i] = filter;
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