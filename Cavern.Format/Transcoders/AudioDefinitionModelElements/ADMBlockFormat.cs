using System.Numerics;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// One position of an object's movement.
    /// </summary>
    public sealed class ADMBlockFormat {
        /// <summary>
        /// Position in this timeslot.
        /// </summary>
        /// <remarks>Cavern and ADM have their y/z axes swapped. This property is in Cavern's coordinate system.</remarks>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Timeslot offset.
        /// </summary>
        public ADMTimeSpan Offset { get; set; }

        /// <summary>
        /// Length of the timeslot.
        /// </summary>
        public ADMTimeSpan Duration { get; set; }

        /// <summary>
        /// Time to take to fade to the next position.
        /// </summary>
        public ADMTimeSpan Interpolation { get; set; }

        /// <summary>
        /// Display block information on string conversion.
        /// </summary>
        public override string ToString() => $"({Position.X}; {Position.Y}; {Position.Z}), {Offset} + {Duration}";
    }
}