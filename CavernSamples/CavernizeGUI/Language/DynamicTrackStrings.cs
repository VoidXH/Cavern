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
        public override string NotSupported => (string)source["NoSup"] ?? base.NotSupported;

        /// <inheritdoc/>
        public override string TypeEAC3JOC => (string)source["E3JOC"] ?? base.TypeEAC3JOC;

        /// <inheritdoc/>
        public override string ObjectBasedTrack => (string)source["ObTra"] ?? base.ObjectBasedTrack;

        /// <inheritdoc/>
        public override string ChannelBasedTrack => (string)source["ChTra"] ?? base.ChannelBasedTrack;

        /// <inheritdoc/>
        public override string SourceChannels => (string)source["SouCh"] ?? base.SourceChannels;

        /// <inheritdoc/>
        public override string MatrixedBeds => (string)source["MatBe"] ?? base.MatrixedBeds;

        /// <inheritdoc/>
        public override string MatrixedObjects => (string)source["MatOb"] ?? base.MatrixedObjects;

        /// <inheritdoc/>
        public override string BedChannels => (string)source["SouBe"] ?? base.BedChannels;

        /// <inheritdoc/>
        public override string DynamicObjects => (string)source["SouDy"] ?? base.DynamicObjects;

        /// <inheritdoc/>
        public override string Channels => (string)source["Chans"] ?? base.Channels;

        /// <inheritdoc/>
        public override string WithObjects => (string)source["WiObj"] ?? base.WithObjects;

        /// <inheritdoc/>
        public override string InvalidTrack => (string)source["InvTr"] ?? base.InvalidTrack;

        /// <inheritdoc/>
        public override string Later => (string)source["Later"] ?? base.Later;

        /// <inheritdoc/>
        protected override IReadOnlyDictionary<Codec, string> GetCodecNames() => (string)source["NoSup"] != null ?
            new Dictionary<Codec, string> {
                { Codec.PCM_Float, (string)source["PCM_Float"] },
                { Codec.PCM_LE, (string)source["PCM_LE"] },
            } :
            base.GetCodecNames();
    }
}
