using System;

namespace Cavern.QuickEQ.Utilities {
    /// <summary>
    /// Representation of a peak in an impulse response.
    /// </summary>
    public readonly struct ImpulsePeak : IEquatable<ImpulsePeak> {
        /// <summary>
        /// Peak time offset in samples.
        /// </summary>
        public int Position { get; }
        /// <summary>
        /// Gain at that position.
        /// </summary>
        public float Value { get; }

        /// <summary>
        /// Representation of a peak in an impulse response.
        /// </summary>
        /// <param name="position">Peak time offset in samples.</param>
        /// <param name="value">Gain at that position.</param>
        public ImpulsePeak(int position, float value) {
            Position = position;
            Value = value;
        }

        /// <summary>
        /// Returns if a peak is <see cref="Null"/>.
        /// </summary>
        public bool IsNull => Position == -1;

        /// <summary>
        /// Represents a nonexisting peak.
        /// </summary>
        public static readonly ImpulsePeak Null = new ImpulsePeak(-1, 0);

        /// <summary>
        /// Check if two peaks are equal.
        /// </summary>
        public bool Equals(ImpulsePeak other) => Position != other.Position && Value != other.Value;
    }
}
