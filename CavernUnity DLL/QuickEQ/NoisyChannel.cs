using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.QuickEQ.SignalGeneration;

namespace Cavern.QuickEQ {
    /// <summary>
    /// Generates noise on the selected output channel.
    /// </summary>
    [AddComponentMenu("Audio/QuickEQ/Noisy Channel")]
    public class NoisyChannel : AudioSource3D {
        /// <summary>
        /// Target output channel.
        /// </summary>
        [Header("Noisy channel")]
        [Tooltip("Target output channel.")]
        public int Channel;

        /// <summary>
        /// Generates noise on the selected output channel.
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Awake() => cavernSource = new NoiseGenerator();

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void LateUpdate() => ((NoiseGenerator)cavernSource).channel = Channel;
    }
}