using System.Diagnostics.CodeAnalysis;
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
        /// The convolution clip containing impulse responses for each output channel.
        /// Must have the same channel count as <see cref="Listener.Channels"/>.
        /// </summary>
        [Header("Convolved listener")]
        [Tooltip("Convolution clip containing an impulse response for each output channel. " +
            "Its channel count must match the output layout.")]
        public Clip ConvolutionClip {
            get => CavernListener.ConvolutionClip;
            set => CavernListener.ConvolutionClip = value;
        }

        /// <summary>
        /// The wrapped <see cref="ConvolvedListener"/> handled by this component.
        /// </summary>
        ConvolvedListener CavernListener => (ConvolvedListener)cavernListener;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        protected override void Awake() {
            base.Awake();
            cavernListener = new ConvolvedListener(false) {
                LimiterOnly = true
            };
        }
    }
}
