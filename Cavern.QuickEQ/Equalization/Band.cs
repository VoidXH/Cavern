using System;

namespace Cavern.QuickEQ.Equalization {
    /// <summary>
    /// A single equalizer band.
    /// </summary>
    public readonly struct Band : IComparable<Band>, IEquatable<Band> {
        /// <summary>
        /// Position of the band in Hz.
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
        /// Check if two bands are at the same frequency and gain.
        /// </summary>
        public static bool operator ==(Band lhs, Band rhs) => lhs.Equals(rhs);

        /// <summary>
        /// Check if two bands are at different frequencies or gains.
        /// </summary>
        public static bool operator !=(Band lhs, Band rhs) => lhs.Frequency != rhs.Frequency || lhs.Gain != rhs.Gain;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Band other ?
            Equals(other) :
            base.Equals(obj);


        /// <inheritdoc/>
        public override int GetHashCode() => Frequency.GetHashCode() + Gain.GetHashCode();

        /// <summary>
        /// Band data as text.
        /// </summary>
        public override string ToString() => $"{Frequency} Hz: {Gain} dB";

        /// <summary>
        /// Compare bands by frequency.
        /// </summary>
        public int CompareTo(Band other) => Frequency.CompareTo(other.Frequency);

        /// <inheritdoc/>
        public bool Equals(Band other) => Frequency == other.Frequency && Gain == other.Gain;
    }
}