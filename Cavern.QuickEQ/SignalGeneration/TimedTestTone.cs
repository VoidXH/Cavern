using System;

namespace Cavern.QuickEQ.SignalGeneration {
    /// <summary>Plays a test tone on a single channel, with a delay to let channels with a smaller ID play first.
    /// An instance has to be created for all channels for complete system measurement.</summary>
    public class TimedTestTone : Source {
        /// <summary>Pregenerated test tone samples.</summary>
        readonly float[] testTone;
        /// <summary>Target output channel.</summary>
        readonly int channel = 0;

        /// <summary>Rendered output array kept to save allocation time.</summary>
        float[] rendered = new float[0];

        /// <summary>Create the source from any waveform.</summary>
        public TimedTestTone(int channel, float[] testTone) {
            this.channel = channel;
            this.testTone = testTone;
        }

        /// <summary>Creates a cache and always marks this source for playback.</summary>
        protected override bool Precollect() {
            if (rendered.Length != Listener.Channels.Length * listener.UpdateRate)
                rendered = new float[Listener.Channels.Length * listener.UpdateRate];
            return true;
        }

        /// <summary>Generates the tone and returns a mix to be added to the output.</summary>
        protected override float[] Collect() {
            Array.Clear(rendered, 0, rendered.Length);
            if (IsPlaying && !Mute && testTone != null) {
                int delay = channel * testTone.Length, channels = Listener.Channels.Length;
                int pos = TimeSamples - delay;
                for (int sample = channel; sample < rendered.Length; sample += channels) {
                    if (pos < 0) {
                        ++pos; // TODO: optimize
                        continue;
                    }
                    if (pos >= testTone.Length) {
                        Array.Clear(rendered, sample, rendered.Length - sample);
                        IsPlaying = false;
                        break;
                    }
                    rendered[sample] = testTone[pos];
                    ++pos;
                }
                TimeSamples += listener.UpdateRate;
            }
            return rendered;
        }
    }
}