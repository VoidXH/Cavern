using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Extra vector calculations.
    /// </summary>
    public static class VectorExtensions {
        /// <summary>
        /// Converts degrees to radians. = pi / 180.
        /// </summary>
        internal const float Deg2Rad = .01745329251f;

        /// <summary>
        /// Converts radians to degrees. = 180 / pi.
        /// </summary>
        internal const float Rad2Deg = 57.295779513f;

        /// <summary>
        /// sqrt(2) / 2 = 1 / sqrt(2)
        /// </summary>
        internal const float Sqrt2p2 = .7071067811f;

        /// <summary>
        /// Returns a vector that's the same direction as the source vector, but it's on the side of a 2x2x2 cube.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MapToCube(this Vector3 vector) {
            float max = Math.Max(Math.Abs(vector.X), Math.Max(Math.Abs(vector.Y), Math.Abs(vector.Z)));
            return vector / max;
        }

        /// <summary>
        /// Returns a vector that's the same direction as the source vector, but has a length of 1.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normalized(this Vector3 vector) => vector / vector.Length();

        /// <summary>
        /// For given angles (in degrees) it returns a vector for that position on a cube with the side length of 2.
        /// </summary>
        public static Vector3 PlaceInCube(this Vector3 angles) {
            float xRad = angles.X * Deg2Rad,
                yRad = angles.Y * Deg2Rad,
                sinX = (float)Math.Sin(xRad),
                cosX = (float)Math.Cos(xRad),
                sinY = (float)Math.Sin(yRad),
                cosY = (float)Math.Cos(yRad);
            if (Math.Abs(sinY) > Math.Abs(cosY)) {
                sinY = Math.Sign(sinY) * Sqrt2p2;
            } else {
                cosY = Math.Sign(cosY) * Sqrt2p2;
            }
            sinY /= Sqrt2p2;
            cosY /= Sqrt2p2;
            if (Math.Abs(sinX) >= Sqrt2p2) {
                sinX = Math.Sign(sinX) * Sqrt2p2;
                cosX /= Sqrt2p2;
                sinY *= cosX;
                cosY *= cosX;
            }
            sinX /= Sqrt2p2;
            return new Vector3(sinY, -sinX, cosY);
        }

        /// <summary>
        /// For given angles (in degrees) it returns a vector for that position on a sphere with the radius of 1.
        /// </summary>
        public static Vector3 PlaceInSphere(this Vector3 angles) {
            float xRad = angles.X * Deg2Rad, yRad = angles.Y * Deg2Rad, cosX = (float)Math.Cos(xRad),
                sinY = (float)Math.Sin(yRad), cosY = (float)Math.Cos(yRad);
            return new Vector3(sinY * cosX, -(float)Math.Sin(xRad), cosY * cosX);
        }

        /// <summary>
        /// Depending on the room shape (spherical or cube), returns the position on the normalized shape.
        /// To get the actual wall position, scale with <see cref="Listener.EnvironmentSize"/>.
        /// </summary>
        public static Vector3 PlaceOnRoom(this Vector3 angles) {
            if (Listener.IsSpherical) {
                return PlaceInSphere(angles);
            }
            return PlaceInCube(angles);
        }

        /// <summary>
        /// Converts a normalized direction vector to the normalized shape of the room.
        /// To get the actual wall position, scale with <see cref="Listener.EnvironmentSize"/>.
        /// </summary>
        public static Vector3 PlaceNormalOnRoom(this Vector3 normal) {
            if (Listener.IsSpherical) {
                return normal;
            }
            return normal.WarpNormalToCube();
        }

        /// <summary>
        /// Rotate this vector by all axes in the opposite direction.
        /// </summary>
        public static Vector3 RotateInverse(this Vector3 vector, Vector3 angles) {
            angles *= -Deg2Rad;

            float cos = (float)Math.Cos(angles.Z),
                sin = (float)Math.Sin(angles.Z),
                x = cos * vector.X - sin * vector.Y,
                y = sin * vector.X + cos * vector.Y;

            cos = (float)Math.Cos(angles.Y);
            sin = (float)Math.Sin(angles.Y);
            float z = cos * vector.Z - sin * x;
            x = cos * x + sin * vector.Z;

            cos = (float)Math.Cos(angles.X);
            sin = (float)Math.Sin(angles.X);
            float oldY = y;
            y = cos * y - sin * z;
            z = sin * oldY + cos * z;

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Warps the points of a sphere to points a cube.
        /// </summary>
        public static Vector3 WarpToCube(this Vector3 vector) {
            float length = vector.Length();
            float max = Math.Max(Math.Abs(vector.X), Math.Max(Math.Abs(vector.Y), Math.Abs(vector.Z)));
            return vector * length / max;
        }

        /// <summary>
        /// Warps the points of a normalized sphere to points a cube. Requires the length of the vector to be 1.
        /// </summary>
        public static Vector3 WarpNormalToCube(this Vector3 vector) {
            float max = Math.Max(Math.Abs(vector.X), Math.Max(Math.Abs(vector.Y), Math.Abs(vector.Z)));
            return vector / max;
        }
    }
}