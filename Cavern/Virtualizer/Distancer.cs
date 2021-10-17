using Cavern.Filters;

namespace Cavern.Virtualizer {
    /// <summary>Simulates distance for objects when enabled and using virtualization.</summary>
    public class Distancer : Filter {
        /// <summary>Precalculator and impulse generator object.</summary>
        readonly DistancerMaster master;
        /// <summary>Convolution used for actual filtering.</summary>
        readonly SpikeConvolver filter;

        /// <summary>Create a distance simulation following a <see cref="DistancerMaster"/>.</summary>
        public Distancer(DistancerMaster master) {
            this.master = master;
            filter = new SpikeConvolver(new float[1] { 1 }, 0);
        }

        /// <summary>Apply distance simulation on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            if (Listener.Channels[channel].Y < 0)
                filter.Impulse = master.LeftFilter;
            else
                filter.Impulse = master.RightFilter;
            filter.Process(samples, channel, channels);
        }
    }
}