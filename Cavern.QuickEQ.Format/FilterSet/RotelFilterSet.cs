using System;
using System.IO;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format.FilterSet.Enums;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Generates PEQ sets for Rotel devices.
    /// </summary>
    public class RotelFilterSet : IIRFilterSet {
        /// <inheritdoc/>
        public override DelayUnit DelayUnits => DelayUnit.Centimeters;

        /// <inheritdoc/>
        public override int Bands => 10;

        /// <inheritdoc/>
        public override double MinGain => -12;

        /// <inheritdoc/>
        public override double MaxGain => 2;

        /// <inheritdoc/>
        public override double GainPrecision => 1;

        /// <inheritdoc/>
        public override double CenterQ => 5;

        /// <inheritdoc/>
        public override double QPrecision => 1;

        /// <summary>
        /// Generates PEQ sets for Rotel devices.
        /// </summary>
        public RotelFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Generates PEQ sets for Rotel devices.
        /// </summary>
        public RotelFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        public override void PostprocessFilter(PeakingEQ filter) {
            base.PostprocessFilter(filter);
            if (filter.CenterFreq < 100) {
                filter.CenterFreq = Math.Round(filter.CenterFreq);
            } else if (filter.CenterFreq < 2000) {
                filter.CenterFreq = Math.Round(filter.CenterFreq / 10) * 10;
            } else {
                filter.CenterFreq = Math.Round(filter.CenterFreq   / 100) * 100;
            }
        }

        /// <inheritdoc/>
        public override void Export(string path) => File.WriteAllText(path, Export());
    }
}
