using System;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>
    /// A single equalizer band.
    /// </summary>
    public readonly struct Band : IComparable<Band> {
        /// <summary>
        /// Position of the band.
        /// </summary>
        public readonly double Frequency { get; }
        /// <summary>
        /// Gain at <see cref="Frequency"/> in decibels.
        /// </summary>
        public readonly double Gain { get; }

        /// <summary>
        /// EQ band constructor.
        /// </summary>
        public Band(double frequency, double gain) {
            Frequency = frequency;
            Gain = gain;
        }

        /// <summary>
        /// Add gain in decibels to this band.
        /// </summary>
        public static Band operator +(Band band, double gain) => new Band(band.Frequency, band.Gain + gain);

        /// <summary>
        /// Subtract gain in decibels from this band.
        /// </summary>
        public static Band operator -(Band band, double gain) => new Band(band.Frequency, band.Gain - gain);

        /// <summary>
        /// Set gain for this band.
        /// </summary>
        public static Band operator *(Band band, double gain) => new Band(band.Frequency, band.Gain * gain);

        /// <summary>
        /// Band data as text.
        /// </summary>
        public override string ToString() => $"{Frequency} Hz: {Gain} dB";

        /// <summary>
        /// Compare bands by frequency.
        /// </summary>
        public int CompareTo(Band other) => Frequency.CompareTo(other.Frequency);
    }
}