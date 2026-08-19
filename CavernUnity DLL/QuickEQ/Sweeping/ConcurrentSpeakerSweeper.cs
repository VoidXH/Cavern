using System;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.QuickEQ.Sweeping {
    /// <summary>
    /// Measures the frequency response of selected output channels at the same time and returns a measurement with all of them excited.
    /// </summary>
    [AddComponentMenu("Audio/QuickEQ/Concurrent Speaker Sweeper")]
    public class ConcurrentSpeakerSweeper : SpeakerSweeper, IDisposable {
        /// <summary>
        /// Channel indices to sweep.
        /// </summary>
        [Tooltip("Channel indices to sweep.")]
        public int[] ChannelIndices;

        /// <summary>
        /// Delay each signal at the given <see cref="ChannelIndices"/> by this many samples. Subsample delay is allowed.
        /// </summary>
        [Tooltip("Delay each signal at the given channel indices by this many samples. Subsample delay is allowed.")]
        public float[] Delays;

        /// <summary>
        /// Per-channel delayed sweep copies.
        /// </summary>
        float[][] channelSweeps;

        /// <summary>
        /// Get the measurement signal for a specific channel, applying the subsample delay.
        /// </summary>
        /// <param name="channel">The index into <see cref="ChannelIndices"/>.</param>
        /// <returns>The delayed sweep samples for the given channel.</returns>
        public override float[] GetSweepForChannel(int channel) => channelSweeps[channel];

        /// <inheritdoc/>
        public override void RegenerateSweep() {
            base.RegenerateSweep();
            int channels = ChannelIndices.Length;
            channelSweeps = new float[Listener.Channels.Length][];
            for (int i = 0; i < channels; i++) {
                int channelIndex = ChannelIndices[i];
                channelSweeps[channelIndex] = SweepReference.FastClone();
                WaveformUtils.Delay(channelSweeps[channelIndex], Delays[i]);
            }

            float[] silence = null;
            for (int i = 0; i < channelSweeps.Length; i++) {
                if (channelSweeps[i] == null) {
                    channelSweeps[i] = silence ??= new float[SweepReference.Length];
                }
            }
        }
    }
}
