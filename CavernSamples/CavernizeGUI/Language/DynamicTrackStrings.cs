using System.Collections.Generic;
using System.Windows;

using Cavern.Format.Common;

using Cavernize.Logic.Language;

namespace CavernizeGUI.Language {
    /// <summary>
    /// Reads the <see cref="TrackStrings"/> from Cavernize GUI's localized resources.
    /// </summary>
    public sealed class DynamicTrackStrings : TrackStrings {
        /// <summary>
        /// Cavernize GUI's localized <see cref="TrackStrings"/>.
        /// </summary>
        readonly ResourceDictionary source = Consts.Language.GetTrackStrings();

        /// <inheritdoc/>
        public override string NotSupported => (string)source["NoSup"];

        /// <inheritdoc/>
        public override string TypeEAC3JOC => (string)source["E3JOC"];

        /// <inheritdoc/>
        public override string ObjectBasedTrack => (string)source["ObTra"];

        /// <inheritdoc/>
        public override string ChannelBasedTrack => (string)source["ChTra"];

        /// <inheritdoc/>
        public override string SourceChannels => (string)source["SouCh"];

        /// <inheritdoc/>
        public override string MatrixedBeds => (string)source["MatBe"];

        /// <inheritdoc/>
        public override string MatrixedObjects => (string)source["MatOb"];

        /// <inheritdoc/>
        public override string BedChannels => (string)source["SouBe"];

        /// <inheritdoc/>
        public override string DynamicObjects => (string)source["SouDy"];

        /// <inheritdoc/>
        public override string Channels => (string)source["Chans"];

        /// <inheritdoc/>
        public override string WithObjects => (string)source["WiObj"];

        /// <inheritdoc/>
        protected override IReadOnlyDictionary<Codec, string> GetCodecNames() => new Dictionary<Codec, string>() {
            { Codec.PCM_Float, (string)source["PCM_Float"] },
            { Codec.PCM_LE, (string)source["PCM_LE"] },
        };
    }
}
