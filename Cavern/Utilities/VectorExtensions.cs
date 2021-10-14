using System;
using System.Numerics;

namespace Cavern.Utilities {
    /// <summary>Extra vector calculations.</summary>
    public static class VectorExtensions {
        /// <summary>Converts degrees to radians. = pi / 180.</summary>
        internal const float Deg2Rad = .01745329251f;
        /// <summary>Converts radians to degrees. = 180 / pi.</summary>
        internal const float Rad2Deg = 57.295779513f;
        /// <summary>sqrt(2) / 2 = 1 / sqrt(2)</summary>
        internal const float Sqrt2p2 = .7071067811f;

        /// <summary>For given angles (in degrees) it returns a vector for that position on a cube with the side length of 2.</summary>
        public static Vector3 PlaceInCube(this Vector3 angles) {
            float xRad = angles.X * Deg2Rad,
                yRad = angles.Y * Deg2Rad,
                sinX = (float)Math.Sin(xRad),
                cosX = (float)Math.Cos(xRad),
                sinY = (float)Math.Sin(yRad),
                cosY = (float)Math.Cos(yRad);
            if (Math.Abs(sinY) > Math.Abs(cosY))
                sinY = Math.Sign(sinY) * Sqrt2p2;
            else
                cosY = Math.Sign(cosY) * Sqrt2p2;
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

        /// <summary>For given angles (in degrees) it returns a vector for that position on a sphere with the radius of 1.</summary>
        public static Vector3 PlaceInSphere(this Vector3 angles) {
            float xRad = angles.X * Deg2Rad, yRad = angles.Y * Deg2Rad, cosX = (float)Math.Cos(xRad),
                sinY = (float)Math.Sin(yRad), cosY = (float)Math.Cos(yRad);
            return new Vector3(sinY * cosX, -(float)Math.Sin(xRad), cosY * cosX);
        }

        /// <summary>Rotate this vector by all axes in the opposite direction.</summary>
        public static Vector3 RotateInverse(this Vector3 vector, Vector3 angles) {
            angles *= -Deg2Rad;

            float cos = (float)Math.Cos(angles.Z),
                sin = (float)Math.Sin(angles.Z),
                x = cos * vector.X - sin * vector.Y,
                y = sin * x + cos * vector.Y;

            cos = (float)Math.Cos(angles.Y);
            sin = (float)Math.Sin(angles.Y);
            x = cos * x + sin * angles.Z;
            float z = cos * angles.Z - sin * x;

            cos = (float)Math.Cos(angles.X);
            sin = (float)Math.Sin(angles.X);
            y = cos * y - sin * z;
            z = sin * y + cos * z;

            return new Vector3(x, y, z);
        }
    }
}