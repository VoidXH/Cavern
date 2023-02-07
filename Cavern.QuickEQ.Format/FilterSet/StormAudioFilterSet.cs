namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for StormAudio hardware.
    /// </summary>
    public class StormAudioFilterSet : IIRFilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 12;

        /// <summary>
        /// Minimum gain of a single peaking EQ band.
        /// </summary>
        public override double MinGain => -18;

        /// <summary>
        /// Maximum gain of a single peaking EQ band.
        /// </summary>
        public override double MaxGain => 18;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public override double GainPrecision => .5;

        /// <summary>
        /// IIR filter set for StormAudio processors with a given number of channels.
        /// </summary>
        public StormAudioFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected override string GetLabel(int channel) => channel > 7 ? base.GetLabel(channel) :
            Channels.Length < 7 ? labels51[channel] : labels71[channel];

        /// <summary>
        /// 5.1 layout labels in order for StormAudio hardware.
        /// </summary>
        static readonly string[] labels51 = { "LF", "RF", "CF", "SUB", "LS", "RS" };

        /// <summary>
        /// 7.1 layout labels in order for StormAudio hardware.
        /// </summary>
        static readonly string[] labels71 = { "LF", "RF", "CF", "SUB", "LB", "RB", "LS", "RS" };
    }
}