using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Performs convolution on an interlaced multichannel signal.
    /// </summary>
    public class MultichannelConvolver : Filter {
        /// <summary>
        /// Each channel's actual filter and reuseable extracted sample block cache.
        /// </summary>
        readonly ThreadSafeFastConvolver[] workers;

        /// <summary>
        /// Performs the convolution for each channel on a different thread.
        /// </summary>
        readonly Parallelizer threader;

        /// <summary>
        /// The samples the <see cref="threader"/> works on.
        /// </summary>
        float[] samples;

        /// <summary>
        /// Performs convolution on an interlaced multichannel signal.
        /// </summary>
        /// <param name="impulses">The convolution filters' impulse responses for each channel that will be present in the signal
        /// that will be used to call <see cref="Filter.Process(float[])"/> with.</param>
        public MultichannelConvolver(MultichannelWaveform impulses) {
            workers = new ThreadSafeFastConvolver[impulses.Channels];
            for (int i = 0; i < workers.Length; i++) {
                workers[i] = new ThreadSafeFastConvolver(impulses[i]);
            }
            threader = new Parallelizer(Step);
        }

        /// <summary>
        /// Performs the convolution of multiple real signal pairs of any length. The real result is returned.
        /// </summary>
        public static MultichannelWaveform ConvolveSafe(MultichannelWaveform excitations, MultichannelWaveform impulses) {
            float[][] results = new float[excitations.Channels][];
            Parallelizer.For(0, results.Length, i => {
                results[i] = FastConvolver.ConvolveSafe(excitations[i], impulses[i]);
            });
            return new MultichannelWaveform(results);
        }

        /// <summary>
        /// Apply the convolution filter of each channel on a multichannel signal with a matching channel count.
        /// </summary>
        public override void Process(float[] samples) {
            this.samples = samples;
            threader.For(0, workers.Length);
        }

        /// <summary>
        /// Apply a single channel's filter only on a multichannel signal's same channel.
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) =>
            workers[channel].Process(samples, channel, channels);

        /// <summary>
        /// The operation the <see cref="workers"/> perform.
        /// </summary>
        void Step(int channel) => workers[channel].Process(samples, channel, workers.Length);
    }
}