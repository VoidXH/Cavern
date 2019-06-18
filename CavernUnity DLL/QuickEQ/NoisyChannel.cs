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

        /// <summary>Custom Cavern <see cref="Source"/> for this component.</summary>
        class NoisyChannelSource : Source {
            /// <summary>Target output channel.</summary>
            public int channel = 0;

            /// <summary>Rendered output array kept to save allocation time.</summary>
            float[] rendered = new float[0];
            /// <summary>Random number generator.</summary>
            Random generator = new Random();

            protected override bool Precollect() {
                int renderBufferSize = Listener.Channels.Length * AudioListener3D.Current.UpdateRate;
                if (rendered.Length != renderBufferSize)
                    rendered = new float[renderBufferSize];
                return true;
            }

            protected override float[] Collect() {
                Array.Clear(rendered, 0, rendered.Length);
                if (IsPlaying && !Mute) {
                    int channels = Listener.Channels.Length;
                    if (channel < 0 || channel >= channels) {
                        UnityEngine.Debug.LogError(string.Format("Incorrect channel: {0}", channel));
                        return rendered;
                    }
                    float gain = Volume * 2;
                    for (int sample = channel; sample < rendered.Length; sample += channels)
                        rendered[sample] = (float)generator.NextDouble() * gain - Volume;
                }
                return rendered;
            }
        }

        /// <summary>Generates noise on the selected output channel.</summary>
        void Awake() => cavernSource = new NoisyChannelSource();

        void LateUpdate() => ((NoisyChannelSource)cavernSource).channel = Channel;
    }
}