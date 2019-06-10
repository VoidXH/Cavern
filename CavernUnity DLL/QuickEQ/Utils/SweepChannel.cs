using System;
using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Runs the sweep of a <see cref="SpeakerSweeper"/> with a correct delay for the given channel.</summary>
    [AddComponentMenu("Audio/QuickEQ/Sweep Channel")]
    internal class SweepChannel : AudioSource3D {
        /// <summary>Target output channel.</summary>
        [Header("Sweep channel")]
        [Tooltip("Target output channel.")]
        public int Channel = 0;
        /// <summary>Sweeper to use the sweep reference of.</summary>
        [Tooltip("Sweeper to use the sweep reference of.")]
        public SpeakerSweeper Sweeper;

        /// <summary>Custom Cavern <see cref="Source"/> for this component.</summary>
        class SweepChannelSource : Source {
            /// <summary>Target output channel.</summary>
            public int Channel = 0;
            /// <summary>Sweeper to use the sweep reference of.</summary>
            public SpeakerSweeper Sweeper;

            /// <summary>Rendered output array kept to save allocation time.</summary>
            float[] rendered = new float[0];

            protected override bool Precollect() {
                int renderBufferSize = Listener.Channels.Length * AudioListener3D.Current.UpdateRate;
                if (rendered.Length != renderBufferSize)
                    rendered = new float[renderBufferSize];
                return true;
            }

            protected override float[] Collect() {
                Array.Clear(rendered, 0, rendered.Length);
                if (IsPlaying && !Mute) {
                    int sweepLength = Sweeper.SweepReference.Length, delay = Channel * sweepLength, channels = Listener.Channels.Length;
                    int pos = TimeSamples - delay;
                    for (int sample = Channel, end = rendered.Length; sample < end; sample += channels) {
                        if (pos < 0)
                            continue;
                        if (pos >= sweepLength) {
                            IsPlaying = false;
                            break;
                        }
                        rendered[sample] = Sweeper.SweepReference[pos];
                        ++pos;
                    }
                    TimeSamples += AudioListener3D.Current.UpdateRate;
                }
                return rendered;
            }
        }

        void Awake() => cavernSource = new SweepChannelSource();

        void Start() {
            SweepChannelSource source = (SweepChannelSource)cavernSource;
            source.Channel = Channel;
            source.Sweeper = Sweeper;
        }
    }
}