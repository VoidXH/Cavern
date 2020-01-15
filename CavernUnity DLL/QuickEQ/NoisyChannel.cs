using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Generates noise on the selected output channel.</summary>
    [AddComponentMenu("Audio/QuickEQ/Noisy Channel")]
    public class NoisyChannel : AudioSource3D {
        /// <summary>Target output channel.</summary>
        [Header("Noisy channel")]
        [Tooltip("Target output channel.")]
        public int Channel = 0;

        /// <summary>Generates noise on the selected output channel.</summary>
        void Awake() => cavernSource = new NoiseGenerator();

        void LateUpdate() => ((NoiseGenerator)cavernSource).channel = Channel;
    }
}