using Cavern.Format.Common;
using Cavern.Format.Container;
using Cavern.Format.Decoders;
using Cavern.Format.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Reads an audio track from a container.
    /// </summary>
    public class AudioTrackReader : AudioReader {
        /// <summary>
        /// Container to read the track from.
        /// </summary>
        readonly ContainerReader source;

        /// <summary>
        /// Not the unique <see cref="Track.ID"/>, but its position in <see cref="source.Tracks"/>.
        /// </summary>
        readonly int track;

        /// <summary>
        /// Decoder based on the <see cref="Codec"/> of the selected stream.
        /// </summary>
        Decoder decoder;

        /// <summary>
        /// Reads an audio track from a container.
        /// </summary>
        /// <param name="source">Container to fetch the tracklist from</param>
        /// <param name="track">Not the unique <see cref="Track.ID"/>,
        /// but its position in <see cref="ContainerReader.Tracks"/>.</param>
        public AudioTrackReader(ContainerReader source, int track) : base(source.reader) {
            this.source = source;
            this.track = track;
        }

        /// <summary>
        /// Fill the file metadata from the selected track.
        /// </summary>
        public override void ReadHeader() {
            if (track >= source.Tracks.Length)
                throw new InvalidTrackException(track, source.Tracks.Length);
            Track selected = source.Tracks[track];
            if (!selected.Format.IsAudio())
                throw new UnsupportedCodecException(true, selected.Format);

            TrackExtraAudio info = selected.Extra as TrackExtraAudio;
            ChannelCount = info.ChannelCount;
            Length = (long)(info.SampleRate * source.Duration);
            SampleRate = (int)info.SampleRate;
            Bits = info.Bits;

            switch (selected.Format) {
                case Codec.DTS:
                    decoder = new DTSCoherentAcousticsDecoder(new BlockBuffer<byte>(ReadNextBlock));
                    break;
                case Codec.PCM_LE:
                case Codec.PCM_Float:
                    decoder = new RIFFWaveDecoder(new BlockBuffer<byte>(ReadNextBlock), Bits);
                    break;
                default:
                    throw new UnsupportedCodecException(true, selected.Format);
            }
        }

        /// <summary>
        /// Read a block of samples.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file. Samples are counted for all channels.</remarks>
        public override void ReadBlock(float[] samples, long from, long to) => decoder.DecodeBlock(samples, from, to - from);

        /// <summary>
        /// Gets the next block from the streamed track.
        /// </summary>
        byte[] ReadNextBlock() => source.ReadNextBlock(track);
    }
}