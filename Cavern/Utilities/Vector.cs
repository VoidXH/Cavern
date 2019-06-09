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

        /// <summary>Get the distance from another vector's position.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Distance(Vector from) {
            float xDist = x - from.x, yDist = y - from.y, zDist = z - from.z;
            return (float)Math.Sqrt(xDist * xDist + yDist * yDist + zDist * zDist);
        }

        /// <summary>Divide this vector with another one by each dimension.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Divide(Vector with) {
            x /= with.x;
            y /= with.y;
            z /= with.z;
        }

        /// <summary>Calculate the dot product with another vector.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Dot(Vector rhs) => x * rhs.x + y * rhs.y + z * rhs.z;

        /// <summary>Normalize this vector.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize() {
            float multiplier = 1 / Magnitude;
            x *= multiplier;
            y *= multiplier;
            z *= multiplier;
        }

        /// <summary>Vector substraction.</summary>
        public static Vector operator-(Vector lhs, Vector rhs) => new Vector(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);

        /// <summary>Scalar multiplication.</summary>
        public static Vector operator *(Vector lhs, float rhs) => new Vector(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
    }
}