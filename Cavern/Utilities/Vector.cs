using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>Three-dimensional vector.</summary>
    public struct Vector : IEquatable<Vector> {
        /// <summary>Converts degrees to radians. = pi / 180.</summary>
        internal const float Deg2Rad = .01745329251f;
        /// <summary>sqrt(2) / 2 = 1 / sqrt(2)</summary>
        internal const float Sqrt2p2 = .7071067811f;

        /// <summary>First coordinate of the vector.</summary>
        public float x;
        /// <summary>Second coordinate of the vector.</summary>
        public float y;
        /// <summary>Third coordinate of the vector.</summary>
        public float z;

        /// <summary>Length of the vector.</summary>
        public float Magnitude => (float)Math.Sqrt(x * x + y * y + z * z);

        /// <summary>Three-dimensional vector with a zero z coordinate.</summary>
        public Vector(float x, float y) {
            this.x = x;
            this.y = y;
            z = 0;
        }

        /// <summary>Three-dimensional vector.</summary>
        public Vector(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>Rotate this vector by the X axis.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateX(float degrees) {
            degrees *= Deg2Rad;
            float cos = (float)Math.Cos(degrees), sin = (float)Math.Sin(degrees);
            y = cos * y - sin * z;
            z = sin * y + cos * z;
        }

        /// <summary>Rotate this vector by the Y axis.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateY(float degrees) {
            degrees *= Deg2Rad;
            float cos = (float)Math.Cos(degrees), sin = (float)Math.Sin(degrees);
            x = cos * x + sin * z;
            z = cos * z - sin * x;
        }

        /// <summary>Rotate this vector by the Z axis.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateZ(float degrees) {
            degrees *= Deg2Rad;
            float cos = (float)Math.Cos(degrees), sin = (float)Math.Sin(degrees);
            x = cos * x - sin * y;
            y = sin * x + cos * y;
        }

        /// <summary>Rotate this vector by the X and Y axes.</summary>
        public void RotateXY(float xDegrees, float yDegrees) {
            RotateY(yDegrees);
            RotateX(xDegrees);
        }

        /// <summary>Rotate this vector by all axes.</summary>
        public void Rotate(float xDegrees, float yDegrees, float zDegrees) {
            RotateZ(zDegrees);
            RotateY(yDegrees);
            RotateX(xDegrees);
        }

        /// <summary>Rotate this vector by all axes.</summary>
        public void Rotate(Vector with) {
            RotateZ(with.z);
            RotateY(with.y);
            RotateX(with.x);
        }

        /// <summary>Rotate this vector by all axes in the opposite direction.</summary>
        public void RotateInverse(Vector with) {
            RotateZ(-with.z);
            RotateY(-with.y);
            RotateX(-with.x);
        }

        /// <summary>Get the distance from another vector's position.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Distance(Vector from) {
            float xDist = x - from.x, yDist = y - from.y, zDist = z - from.z;
            return (float)Math.Sqrt(xDist * xDist + yDist * yDist + zDist * zDist);
        }

        /// <summary>Multiply this vector with another one by each dimension.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Scale(Vector with) {
            x *= with.x;
            y *= with.y;
            z *= with.z;
        }

        /// <summary>Divide this vector with another one by each dimension.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Downscale(Vector with) {
            x /= with.x;
            y /= with.y;
            z /= with.z;
        }

        /// <summary>Calculate the dot product with another vector.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Dot(Vector with) => x * with.x + y * with.y + z * with.z;

        /// <summary>Normalize this vector.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize() {
            float multiplier = 1 / Magnitude;
            x *= multiplier;
            y *= multiplier;
            z *= multiplier;
        }

        /// <summary>Vector addition.</summary>
        public static Vector operator+(Vector lhs, Vector rhs) => new Vector(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);

        /// <summary>Vector substraction.</summary>
        public static Vector operator-(Vector lhs, Vector rhs) => new Vector(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);

        /// <summary>Scalar multiplication.</summary>
        public static Vector operator *(Vector lhs, float rhs) => new Vector(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);

        /// <summary>For given angles (in degrees) it returns a vector for that position on a sphere with the radius of 1.</summary>
        public static Vector PlaceInSphere(Vector angles) {
            float xRad = angles.x * Deg2Rad, yRad = angles.y * Deg2Rad, sinX = (float)Math.Sin(xRad), cosX = (float)Math.Cos(xRad),
                sinY = (float)Math.Sin(yRad), cosY = (float)Math.Cos(yRad);
            return new Vector(sinY * cosX, -sinX, cosY * cosX);
        }

        /// <summary>For given angles (in degrees) it returns a vector for that position on a cube with the side length of 2.</summary>
        public static Vector PlaceInCube(Vector angles) {
            float xRad = angles.x * Deg2Rad, yRad = angles.y * Deg2Rad, sinX = (float)Math.Sin(xRad), cosX = (float)Math.Cos(xRad),
                sinY = (float)Math.Sin(yRad), cosY = (float)Math.Cos(yRad);
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
            return new Vector(sinY, -sinX, cosY);
        }

        /// <summary>Check if two channels are the same.</summary>
        public bool Equals(Vector other) => x == other.x && y == other.y && z == other.z;
    }
}