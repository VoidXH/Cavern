using System;
using System.Numerics;

namespace Cavern.Numerics {
    /// <summary>
    /// Represents a line in 2D space.
    /// </summary>
    public struct Line2D {
        /// <summary>
        /// The slope of the line (rise over run). For vertical lines, this is positive infinity.
        /// </summary>
        public float Slope { get; set; }

        /// <summary>
        /// The y-intercept of the line. For vertical lines, this represents the x-intercept.
        /// </summary>
        public float Intercept { get; set; }

        /// <summary>
        /// Represents a line in 2D space.
        /// </summary>
        public Line2D(float slope, float intercept) {
            Slope = slope;
            Intercept = intercept;
        }

        /// <summary>
        /// Calculates a line from two sampled points of it.
        /// </summary>
        /// <exception cref="ArgumentException">Both points are the same.</exception>
        public Line2D(Vector2 a, Vector2 b) {
            if (a == b) {
                throw new ArgumentException("Line calculation requires two distinct points.");
            }

            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            if (dx == 0) {
                Slope = float.PositiveInfinity;
                Intercept = a.X;
            } else {
                Slope = dy / dx;
                Intercept = a.Y - Slope * a.X;
            }
        }

        /// <summary>
        /// Get the X coordinate for a given Y coordinate on the line. For vertical lines, this returns the x-intercept regardless of Y.
        /// </summary>
        public readonly float GetX(float y) {
            if (float.IsInfinity(Slope)) {
                return Intercept;
            } else {
                return (y - Intercept) / Slope;
            }
        }

        /// <summary>
        /// Get the Y coordinate for a given X coordinate on the line. For vertical lines, this is undefined and returns NaN.
        /// </summary>
        public readonly float GetY(float x) {
            if (float.IsInfinity(Slope)) {
                return float.NaN; // Y is undefined for vertical lines
            } else {
                return Slope * x + Intercept;
            }
        }
    }
}
