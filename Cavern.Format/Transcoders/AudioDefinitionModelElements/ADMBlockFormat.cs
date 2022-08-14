using System.Numerics;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// One position of an object's movement.
    /// </summary>
    public class ADMBlockFormat {
        /// <summary>
        /// Position in this timeslot.
        /// </summary>
        /// <remarks>Cavern and ADM have their y/z axes swapped.</remarks>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Timeslot offset in samples.
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// Length of the timeslot in samples.
        /// </summary>
        public long Duration { get; set; }

        /// <summary>
        /// Number of samples to take to fade to the next position.
        /// </summary>
        public long Interpolation { get; set; }
    }
}