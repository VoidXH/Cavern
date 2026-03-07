using System;
using System.Numerics;

namespace Cavern.Numerics {
    /// <summary>
    /// Represents a 2D circle, defined by its center and radius.
    /// </summary>
    public struct Circle {
        /// <summary>
        /// Center point of the circle.
        /// </summary>
        public Vector2 Center { get; set; }

        /// <summary>
        /// Radius of the circle.
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// Represents a 2D circle, defined by its center and radius.
        /// </summary>
        /// <param name="center">Center point of the circle</param>
        /// <param name="radius">Radius of the circle</param>
        public Circle(Vector2 center, float radius) {
            Center = center;
            Radius = radius;
        }

        /// <summary>
        /// Finds where all the <paramref name="circles"/> intersect, or if there isn't any, a point closest to the edges of all circles.
        /// </summary>
        /// <param name="circles">The circles of which the intersection is to be found</param>
        /// <param name="iterations">The more iterations, the more accurate result, 10 is too much</param>
        public static Vector2 Intersect(Circle[] circles, int iterations) {
            if (circles.Length < 2) {
                return circles.Length == 0 ?
                    Vector2.Zero :
                    circles[0].Center;
            }

            Vector2 result = Vector2.Zero;
            for (int i = 0; i < circles.Length; i++) {
                result += circles[i].Center;
            }
            result /= circles.Length; // Initial guess: the average of the centers

            // Modified geometric median algorithm: the error function is to the edge, not to the center
            for (int i = 0; i < iterations; i++) {
                Vector2 newResult = Vector2.Zero;
                float newResultDivisor = 0;
                for (int circle = 0; circle < circles.Length; circle++) {
                    float distance = Vector2.Distance(result, circles[circle].Center) - circles[circle].Radius;
                    if (distance < eps) {
                        continue;
                    }
                    float divisor = 1 / distance;
                    newResult += circles[circle].Center * divisor;
                    newResultDivisor += divisor;
                }

                if (newResultDivisor < eps) {
                    return result;
                }
                result = newResult / newResultDivisor;
            }

            return result;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => $"Circle, center: {Center}, radius: {Radius}.";

        /// <summary>
        /// Finds the point of intersection(s), or if there isn't any, a point right between the edges of the circles.
        /// </summary>
        public readonly Vector2[] Intersect(Circle other) {
            Vector2 direction = other.Center - Center;
            float distanceSq = direction.LengthSquared();
            float distance = MathF.Sqrt(distanceSq);
            float distanceDiv = 1 / distance;

            // No intersection (circles are too far apart or one is inside another)
            float totalRadius = Radius + other.Radius;
            if (distance > totalRadius || distance < MathF.Abs(Radius - other.Radius)) {
                // Return a point right between the edges of the circles
                float midDistance = Radius + (distance - totalRadius) / 2f;
                return new[] { Center + direction * distanceDiv * midDistance };
            }

            float a = (Radius * Radius - other.Radius * other.Radius + distanceSq) / (2 * distance);
            float h = MathF.Sqrt(MathF.Max(0, Radius * Radius - a * a));
            Vector2 p2 = Center + direction * a * distanceDiv; // The line through the intersection points crosses the line between the circle centers here

            // If h is 0, they touch at exactly one point
            if (h < eps) {
                return new[] { p2 };
            }

            // Calculate the two intersection points
            float rx = -direction.Y * (h * distanceDiv);
            float ry = direction.X * (h * distanceDiv);

            return new[] {
                new Vector2(p2.X + rx, p2.Y + ry),
                new Vector2(p2.X - rx, p2.Y - ry)
            };
        }

        /// <summary>
        /// Numbers lower than this are rounding errors.
        /// </summary>
        const float eps = 1e-6f;
    }
}
