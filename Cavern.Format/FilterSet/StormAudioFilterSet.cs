using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for StormAudio hardware.
    /// </summary>
    public class StormAudioFilterSet : GenericFilterSet {
        /// <summary>
        /// Maximum number of EQ bands per channel.
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
        protected override string GetLabel(int channel) => Channels[channel].name ?? (channel > 7 ? "CH" + (channel + 1) :
            Channels.Length < 7 ? labels51[channel] : labels71[channel]);

        /// <summary>
        /// 5.1 layout labels in order for StormAudio hardware.
        /// </summary>
        static readonly string[] labels51 = new string[] { "LF", "RF", "CF", "SUB", "LS", "RS" };

        /// <summary>
        /// 7.1 layout labels in order for StormAudio hardware.
        /// </summary>
        static readonly string[] labels71 = new string[] { "LF", "RF", "CF", "SUB", "LB", "RB", "LS", "RS" };
    }
}