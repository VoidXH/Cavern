using System;
using UnityEngine;

using Random = System.Random;

namespace Cavern.QuickEQ {
    /// <summary>Generates noise on the selected output channel.</summary>
    [AddComponentMenu("Audio/QuickEQ/Noisy Channel")]
    public class NoisyChannel : AudioSource3D {
        /// <summary>Target output channel.</summary>
        [Header("Noisy channel")]
        [Tooltip("Target output channel.")]
        public int Channel = 0;

        /// <summary>Rendered output array kept to save allocation time.</summary>
        float[] Rendered = new float[0];
        /// <summary>Random number generator.</summary>
        Random Generator = new Random();

        internal override bool Precollect() {
            if (Rendered.Length != AudioListener3D.RenderBufferSize)
                Rendered = new float[AudioListener3D.RenderBufferSize];
            return true;
        }

        internal override float[] Collect() {
            Array.Clear(Rendered, 0, Rendered.Length);
            if (IsPlaying && !Mute) {
                int Channels = AudioListener3D.ChannelCount;
                if (Channel < 0 || Channel >= Channels) {
                    UnityEngine.Debug.LogError(string.Format("Incorrect channel: {0}", Channel));
                    return Rendered;
                }
                float Gain = Volume * 2;
                for (int Sample = Channel, End = Rendered.Length; Sample < End; Sample += Channels)
                    Rendered[Sample] = (float)Generator.NextDouble() * Gain - Volume;
            }
            return Rendered;
        }
    }
}