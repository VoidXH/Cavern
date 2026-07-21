using UnityEngine;

namespace Cavern.Listeners {
    /// <summary>
    /// A <see cref="AudioListener3D"/> that applies a convolution set (room correction / EQ) to the output signal.
    /// </summary>
    /// <remarks>This listener wraps a <see cref="ConvolvedListener"/> and applies a multichannel convolution (e.g., room correction) to the rendered output.
    /// The provided <see cref="ConvolutionClip"/> must have the same channel count as the output layout.</remarks>
    [AddComponentMenu("Audio/3D Convolved Audio Listener"), RequireComponent(typeof(AudioListener))]
    public class ConvolvedListener3D : AudioListener3D {
        /// <summary>
        /// The convolution clip containing impulse responses for each output channel. Must have the same channel count as <see cref="Listener.Channels"/>.
        /// </summary>
        [Header("Convolved listener")]
        [Tooltip("Convolution clip containing an impulse response for each output channel. Its channel count must match the output layout.")]
        public Clip ConvolutionClip {
            get => ConvolvedListener.ConvolutionClip;
            set => ConvolvedListener.ConvolutionClip = value;
        }

        /// <summary>
        /// The wrapped <see cref="ConvolvedListener"/> handled by this component.
        /// </summary>
        ConvolvedListener ConvolvedListener => (ConvolvedListener)CavernListener;

        /// <inheritdoc/>
        protected override void Awake() {
            base.Awake();
            CavernListener = new ConvolvedListener(false) {
                LimiterOnly = true
            };
        }
    }
}
