using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for StormAudio hardware.
    /// </summary>
    public class StormAudioFilterSet : IIRFilterSet {
        /// <inheritdoc/>
        public override int Bands => 16;

        /// <inheritdoc/>
        public override double MinGain => -18;

        /// <inheritdoc/>
        public override double MaxGain => 18;

        /// <inheritdoc/>
        public override double GainPrecision => .5;

        /// <summary>
        /// Mandatory marker in each file to be accepted by the processor.
        /// </summary>
        protected override string Header => "Equaliser: StormAudio";

        /// <summary>
        /// IIR filter set for StormAudio processors with a given number of channels.
        /// </summary>
        public StormAudioFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for StormAudio processors with a given set of channels.
        /// </summary>
        public StormAudioFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected override string GetLabel(int channel) => Channels[channel].name ?? Channels[channel].reference switch {
            ReferenceChannel.FrontLeft => "LF",
            ReferenceChannel.FrontRight => "RF",
            ReferenceChannel.FrontCenter => "CF",
            ReferenceChannel.ScreenLFE => "SUB",
            ReferenceChannel.RearLeft => "LB",
            ReferenceChannel.RearRight => "RB",
            ReferenceChannel.SideLeft => "LS",
            ReferenceChannel.SideRight => "RS",
            ReferenceChannel.TopFrontLeft => "LFT",
            ReferenceChannel.TopFrontRight => "RFT",
            ReferenceChannel.TopSideLeft => "LMT",
            ReferenceChannel.TopSideRight => "RMT",
            ReferenceChannel.TopFrontCenter => "CFH",
            ReferenceChannel.GodsVoice => "TOP",
            ReferenceChannel.TopRearLeft => "LBT",
            ReferenceChannel.TopRearRight => "RBT",
            _ => base.GetLabel(channel)
        };
    }
}