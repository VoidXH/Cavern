using System;
using System.Runtime.CompilerServices;

using Cavern.Filters;

namespace Cavern.Listeners {
    /// <summary>
    /// A <see cref="Listener"/> that applies a convolution set (room correction / EQ) to the output signal.
    /// </summary>
    /// <remarks>This listener wraps a base <see cref="Listener"/> and applies a multichannel convolution (e.g., room correction) to the rendered output.
    /// The provided <see cref="ConvolutionClip"/> must have the same channel count as the output layout.</remarks>
    public sealed class ConvolvedListener : Listener {
        /// <summary>
        /// The convolution clip containing impulse responses for each output channel.
        /// Must have the same channel count as <see cref="Listener.Channels"/>.
        /// </summary>
        public Clip ConvolutionClip {
            get => convolutionClip;
            set {
                convolutionClip = value;
                UpdateConvolver();
            }
        }
        Clip convolutionClip;

        /// <summary>
        /// The multichannel convolver that applies the impulse responses to each output channel.
        /// </summary>
        MultichannelConvolver convolver;

        /// <summary>
        /// Center of a listening space. Attached <see cref="Source"/>s will be rendered relative to this object's position.
        /// The layout set up by the user will be used.
        /// </summary>
        public ConvolvedListener() { }

        /// <summary>
        /// Center of a listening space. Attached <see cref="Source"/>s will be rendered relative to this object's position.
        /// </summary>
        /// <param name="loadGlobals">Load the global settings for all listeners. This should be false for listeners created
        /// on the fly, as this overwrites previous application settings that might have been modified.</param>
        public ConvolvedListener(bool loadGlobals) : base(loadGlobals) { }

        /// <summary>
        /// Updates the internal convolver when the convolution clip changes.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <see cref="ConvolutionClip"/> channel count doesn't match the output channel count.</exception>
        void UpdateConvolver() {
            if (convolutionClip == null) {
                convolver = null;
            } else if (convolutionClip.Channels != Channels.Length) {
                throw new ArgumentException($"Convolution clip channel count ({convolutionClip.Channels}) must match output channel count ({Channels.Length}).",
                    nameof(ConvolutionClip));
            } else {
                convolver = new MultichannelConvolver(convolutionClip.Data);
            }
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new float[] Render() => Render(1);

        /// <inheritdoc/>
        public override float[] Render(int frames) {
            float[] result = base.Render(frames);
            if (result != null && convolver != null) {
                convolver.Process(result);
            }
            return result;
        }
    }
}
