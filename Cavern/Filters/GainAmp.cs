using System;
using System.ComponentModel;

using Cavern.Filters.Interfaces;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// A version of a <see cref="Gain"/> running in <see cref="CavernAmp"/>.
    /// </summary>
    public partial class GainAmp : FilterAmp, IGainFilter {
        /// <inheritdoc/>
        [DisplayName("Gain (dB)")]
        public double GainValue {
            get => Gain_GetGainValue(Handle);
            set => Gain_SetGainValue(Handle, value);
        }

        /// <inheritdoc/>
        public bool Invert {
            get => Gain_GetInvert(Handle);
            set => Gain_SetInvert(Handle, value);
        }

        /// <summary>
        /// Signal level multiplier filter.
        /// </summary>
        /// <param name="gain">Filter gain in decibels</param>
        public GainAmp(double gain) : base(Gain_Create(gain)) { }

        /// <summary>
        /// Wraps a native filter instance.
        /// </summary>
        GainAmp(IntPtr handle) : base(handle) { }

        /// <inheritdoc/>
        public override object Clone() => new GainAmp(CavernAmp.Filter_Clone(Handle));
    }
}
