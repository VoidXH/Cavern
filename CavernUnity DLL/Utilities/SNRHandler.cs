using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Utilities {
    /// <summary>
    /// Maintains a constant signal-to-noise ratio between the volume of <see cref="signal"/> and <see cref="noise"/>.
    /// </summary>
    public class SNRHandler : MonoBehaviour {
        /// <summary>
        /// Audio that will be louder.
        /// </summary>
        public AudioSource3D[] signal;

        /// <summary>
        /// Audio that will be fainter.
        /// </summary>
        public AudioSource3D[] noise;

        /// <summary>
        /// Ratio between the volume of signal and noise tracks (in gain).
        /// </summary>
        public float snr = 2;

        /// <summary>
        /// The fixed absolute volume level (in gain) of the selected fixed tracks (by <see cref="signalIsFixed"/>).
        /// </summary>
        public float referenceLevel = .25f;

        /// <summary>
        /// If true, the noise will be decreased if SNR increases. If false, signal will be increased if SNR increases.
        /// </summary>
        public bool signalIsFixed;

        /// <inheritdoc/>
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (signal == null || noise == null) {
                return;
            }

            float signalLevel = referenceLevel;
            float noiseLevel = referenceLevel;
            if (signalIsFixed) {
                noiseLevel /= snr;
            } else {
                signalLevel *= snr;
            }

            for (int i = 0; i < signal.Length; i++) {
                signal[i].Volume = signalLevel;
            }
            for (int i = 0; i < noise.Length; i++) {
                noise[i].Volume = noiseLevel;
            }
        }
    }
}
