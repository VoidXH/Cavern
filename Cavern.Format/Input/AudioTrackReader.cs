using Cavern.Format.Common;
using Cavern.Format.Container;
using Cavern.Format.Decoders;
using Cavern.Format.Renderers;
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
        /// Not the unique <see cref="Track.ID"/>, but its position in the <see cref="source"/>'s list of tracks.
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
        /// Reads an audio track from a container.
        /// </summary>
        /// <param name="source">Container to fetch the tracklist from</param>
        /// <param name="codec">Select a track of this codec or throw an exception if it doesn't exist</param>
        public AudioTrackReader(ContainerReader source, Codec codec) : base(source.reader) {
            this.source = source;
            for (int track = 0; track < source.Tracks.Length; ++track) {
                if (source.Tracks[track].Format == codec) {
                    this.track = track;
                    return;
                }
            }
            throw new CodecNotFoundException(codec);
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
                case Codec.EnhancedAC3:
                    decoder = new EnhancedAC3Decoder(new BlockBuffer<byte>(ReadNextBlock));
                    break;
                case Codec.PCM_LE:
                case Codec.PCM_Float:
                    decoder = new RIFFWaveDecoder(new BlockBuffer<byte>(ReadNextBlock), ChannelCount, Length, SampleRate, Bits);
                    break;
                default:
                    decoder = new DummyDecoder(selected.Format, ChannelCount, Length, SampleRate);
                    break;
            }
        }

        /// <summary>
        /// If the stream can be rendered in 3D by Cavern, return a renderer.
        /// </summary>
        public override Renderer GetRenderer() {
            if (decoder == null)
                ReadHeader();
            if (decoder is RIFFWaveDecoder wav)
                return new RIFFWaveRenderer(wav);
            if (decoder is EnhancedAC3Decoder eac3)
                return new EnhancedAC3Renderer(eac3);
            return null;
        }

        /// <summary>
        /// Read a block of samples.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public override void ReadBlock(float[] samples, long from, long to) => decoder.DecodeBlock(samples, from, to - from);

        /// <summary>
        /// Gets the next block from the streamed track.
        /// </summary>
        byte[] ReadNextBlock() => source.ReadNextBlock(track);
    }
}