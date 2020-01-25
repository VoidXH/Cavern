using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern.Utilities {
    /// <summary>Useful vector functions used in multiple classes.</summary>
    public static class VectorUtils {
        /// <summary>Converts a Unity vector to a Cavern vector.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector VectorMatch(Vector3 source) => new Vector(source.x, source.y, source.z);

        /// <summary>Converts a Cavern vector to a Unity vector.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 VectorMatch(Vector source) => new Vector3(source.x, source.y, source.z);

        /// <summary>Checks if a Cavern and Unity vector are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool VectorCompare(Vector cavernVector, Vector3 unityVector) =>
            cavernVector.x == unityVector.x && cavernVector.y == unityVector.y && cavernVector.z == unityVector.z;
    }
}