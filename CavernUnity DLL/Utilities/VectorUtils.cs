using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern.Utilities {
    /// <summary>
    /// Useful vector functions used in multiple classes.
    /// </summary>
    public static class VectorUtils {
        /// <summary>
        /// Converts a Unity vector to a Cavern vector.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector3 VectorMatch(this Vector3 source) => new System.Numerics.Vector3(source.x, source.y, source.z);

        /// <summary>
        /// Converts a Cavern vector to a Unity vector.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 VectorMatch(this System.Numerics.Vector3 source) => new Vector3(source.X, source.Y, source.Z);

        /// <summary>
        /// Checks if a Cavern and Unity vector are equal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool VectorCompare(System.Numerics.Vector3 cavernVector, Vector3 unityVector) =>
            cavernVector.X == unityVector.x && cavernVector.Y == unityVector.y && cavernVector.Z == unityVector.z;
    }
}