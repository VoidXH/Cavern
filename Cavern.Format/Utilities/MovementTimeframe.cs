using System.Numerics;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Contains a snapshot of the movement of a <see cref="Source"/>.
    /// </summary>
    internal readonly struct MovementTimeframe {
        /// <summary>
        /// Position of the <see cref="Source"/> relative to the room size.
        /// </summary>
        public readonly Vector3 position;

        /// <summary>
        /// Relative volume of the object (voltage scale).
        /// </summary>
        public readonly float gain;

        /// <summary>
        /// The timeframe's position in the stream in samples.
        /// </summary>
        public readonly long offset;

        /// <summary>
        /// The movement takes this many samples before the <see cref="offset"/>.
        /// </summary>
        public readonly int fade;

        /// <summary>
        /// Contains a snapshot of the movement of a <see cref="Source"/>.
        /// </summary>
        public MovementTimeframe(Vector3 position, float gain, long offset, int fade) {
            this.position = position;
            this.gain = gain;
            this.offset = offset;
            this.fade = fade;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{position} at {offset} samples, gain: {gain}, fade time: {fade}";
    }
}
