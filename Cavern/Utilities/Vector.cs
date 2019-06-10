using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>Three-dimensional vector.</summary>
    public struct Vector {
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
            degrees *= Utils.Deg2Rad;
            float cos = (float)Math.Cos(degrees), sin = (float)Math.Sin(degrees);
            y = cos * y - sin * z;
            z = sin * y + cos * z;
        }

        /// <summary>Rotate this vector by the Y axis.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateY(float degrees) {
            degrees *= Utils.Deg2Rad;
            float cos = (float)Math.Cos(degrees), sin = (float)Math.Sin(degrees);
            x = cos * x + sin * z;
            z = cos * z - sin * x;
        }

        /// <summary>Rotate this vector by the Z axis.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateZ(float degrees) {
            degrees *= Utils.Deg2Rad;
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
    }
}