using System;

using Cavern.Utilities;

namespace Cavern.QuickEQ.SignalGeneration {
    /// <summary>
    /// Plays a test tone on a single channel, with a delay to let channels with a smaller ID play first.
    /// An instance has to be created for all channels for complete system measurement.
    /// </summary>
    public class TimedTestTone : Source {
        /// <summary>
        /// Pregenerated test tone samples.
        /// </summary>
        readonly float[] testTone;

        /// <summary>
        /// Target output channel.
        /// </summary>
        readonly int channel;

        /// <summary>
        /// Delay playback by this many times the <see cref="testTone"/>'s length. Used to play after preceding measurements.
        /// </summary>
        readonly int delayChannel;

        /// <summary>
        /// Rendered output array kept to save allocation time.
        /// </summary>
        float[] rendered = new float[0];

        /// <summary>
        /// Create the source from any waveform.
        /// </summary>
        public TimedTestTone(int channel, float[] testTone) : this(channel, testTone, false) { }

        /// <summary>
        /// Create the source from any waveform, and add an additional channel of delay for warming up the input.
        /// </summary>
        public TimedTestTone(int channel, float[] testTone, bool warmUpMode) {
            this.channel = channel;
            this.testTone = testTone;
            delayChannel = warmUpMode ? channel + 1 : channel;
        }

        /// <summary>
        /// Creates a cache and always marks this source for playback.
        /// </summary>
        protected override bool Precollect() {
            if (rendered.Length != Listener.Channels.Length * listener.UpdateRate) {
                rendered = new float[Listener.Channels.Length * listener.UpdateRate];
            }
            return true;
        }

        /// <summary>
        /// Generates the tone and returns a mix to be added to the output.
        /// </summary>
        protected override float[] Collect() {
            rendered.Clear();
            if (IsPlaying && !Mute && testTone != null) {
                int delay = delayChannel * testTone.Length, channels = Listener.Channels.Length,
                    pos = TimeSamples - delay,
                    sample = channel;
                if (pos < 0) {
                    sample -= pos * channels;
                    if (sample < rendered.Length) {
                        pos = 0;
                    } else {
                        pos += rendered.Length / channels;
                    }
                }
                for (; sample < rendered.Length; sample += channels) {
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