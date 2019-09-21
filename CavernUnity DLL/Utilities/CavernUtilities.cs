using Cavern.Utilities;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern {
    /// <summary>Useful functions used in multiple classes.</summary>
    public static class CavernUtilities {
        /// <summary>Converts a signal strength (ref = 1) to dB.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float SignalToDb(float amplitude) => 20 * Mathf.Log10(amplitude);

        /// <summary>Converts a dB value (ref = 0) to signal strength.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float DbToSignal(float amplitude) => Mathf.Pow(10, 1/20f * amplitude);

        /// <summary>Converts a Unity vector to a Cavern vector.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector VectorMatch(Vector3 source) => new Vector(source.x, source.y, source.z);

        /// <summary>Converts a Cavern vector to a Unity vector.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 VectorMatch(Vector source) => new Vector3(source.x, source.y, source.z);

        /// <summary>Checks if a Cavern and Unity vector are equal.</summary>
        public static bool VectorCompare(Vector cavernVector, Vector3 unityVector) =>
            cavernVector.x == unityVector.x && cavernVector.y == unityVector.y && cavernVector.z == unityVector.z;
    }
}