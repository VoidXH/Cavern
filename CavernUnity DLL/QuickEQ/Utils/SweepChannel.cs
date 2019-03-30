using System;
using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Runs the sweep of a <see cref="SpeakerSweeper"/> with a correct delay for the given channel.</summary>
    [AddComponentMenu("Audio/QuickEQ/Sweep Channel")]
    public class SweepChannel : AudioSource3D {
        /// <summary>Target output channel.</summary>
        [Tooltip("Target output channel.")]
        public int Channel = 0;
        /// <summary>Sweeper to use the sweep reference of.</summary>
        public SpeakerSweeper Sweeper;

        /// <summary>Rendered output array kept to save allocation time.</summary>
        float[] Rendered = new float[0];

        internal override bool Precollect() {
            if (Rendered.Length != AudioListener3D.RenderBufferSize)
                Rendered = new float[AudioListener3D.RenderBufferSize];
            return true;
        }

        internal override float[] Collect() {
            Array.Clear(Rendered, 0, Rendered.Length);
            if (IsPlaying && !Mute) {
                int SweepLength = Sweeper.SweepReference.Length, Delay = Channel * SweepLength, Channels = AudioListener3D.ChannelCount;
                int Pos = timeSamples - Delay;
                for (int Sample = Channel, EndSample = Rendered.Length; Sample < EndSample; Sample += Channels) {
                    if (Pos < 0)
                        continue;
                    if (Pos >= SweepLength) {
                        IsPlaying = false;
                        break;
                    }
                    Rendered[Sample] = Sweeper.SweepReference[Pos];
                    ++Pos;
                }
                timeSamples += AudioListener3D.Current.UpdateRate;
            }
            return Rendered;
        }
    }
}