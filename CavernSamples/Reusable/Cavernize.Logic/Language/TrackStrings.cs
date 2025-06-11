using Cavern.Format.Common;

namespace Cavernize.Logic.Language {
    /// <summary>
    /// Strings used in the track selection UI. Override to provide custom translations. Summaries are the default translations.
    /// </summary>
    public class TrackStrings {
        /// <summary>
        /// Format unsupported by Cavern
        /// </summary>
        public virtual string NotSupported => "Format unsupported by Cavern";

        /// <summary>
        /// Enhanced AC-3 with Joint Object Coding
        /// </summary>
        public virtual string TypeEAC3JOC => "Enhanced AC-3 with Joint Object Coding";

        /// <summary>
        /// Object-based audio track
        /// </summary>
        public virtual string ObjectBasedTrack => "Object-based audio track";

        /// <summary>
        /// Channel-based audio track
        /// </summary>
        public virtual string ChannelBasedTrack => "Channel-based audio track";

        /// <summary>
        /// Source channels
        /// </summary>
        public virtual string SourceChannels => "Source channels";

        /// <summary>
        /// Matrixed beds
        /// </summary>
        public virtual string MatrixedBeds => "Matrixed beds";

        /// <summary>
        /// Matrixed objects
        /// </summary>
        public virtual string MatrixedObjects => "Matrixed objects";

        /// <summary>
        /// Bed channels
        /// </summary>
        public virtual string BedChannels => "Bed channels";

        /// <summary>
        /// Dynamic objects
        /// </summary>
        public virtual string DynamicObjects => "Dynamic objects";

        /// <summary>
        /// Channels
        /// </summary>
        public virtual string Channels => "Channels";

        /// <summary>
        /// with objects
        /// </summary>
        public virtual string WithObjects => "with objects";

        /// <summary>
        /// Translated names of supported codecs.
        /// </summary>
        public IReadOnlyDictionary<Codec, string> CodecNames => codecNames ??= GetCodecNames();

        /// <summary>
        /// Cached result of <see cref="CodecNames"/>.
        /// </summary>
        IReadOnlyDictionary<Codec, string> codecNames;

        /// <summary>
        /// Store the names of supported codecs.
        /// </summary>
        protected virtual IReadOnlyDictionary<Codec, string> GetCodecNames() => new Dictionary<Codec, string> {
            { Codec.PCM_Float, "PCM (floating point)" },
            { Codec.PCM_LE, "PCM (integer)" },
        };
    }
}
