using System;

namespace QuickEQResultMerger {
    /// <summary>
    /// Calculated correction values for a single channel.
    /// </summary>
    class Measurement : IComparable<Measurement> {
        /// <summary>
        /// Shortened name (label) of the channel.
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Required amplification of the channel's signal in decibels.
        /// </summary>
        public float Gain { get; private set; }

        /// <summary>
        /// Required delay for the channel in milliseconds.
        /// </summary>
        public float Delay { get; private set; }

        /// <summary>
        /// Calculated correction values for a single channel.
        /// </summary>
        public Measurement(string channel, float gain, float delay) {
            Channel = channel;
            Gain = gain;
            Delay = delay;
        }

        /// <summary>
        /// Apply another correction on this channel.
        /// </summary>
        public void Correct(float gainOffset, float delayOffset) {
            Gain -= gainOffset;
            Delay -= delayOffset;
        }

        /// <summary>
        /// Measurements can be sorted by delay. This is used to find the minimum delay.
        /// </summary>
        public int CompareTo(Measurement other) => Delay.CompareTo(other.Delay);
    }
}