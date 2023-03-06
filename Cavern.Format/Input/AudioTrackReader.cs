using Cavern.Format.Common;
using Cavern.Format.Container;
using Cavern.Format.Decoders;
using Cavern.Format.Renderers;
using Cavern.Format.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Reads an audio track from a container.
    /// </summary>
    public class AudioTrackReader : AudioReader, IMetadataSupplier {
        /// <summary>
        /// Get the container that contains this track.
        /// </summary>
        public ContainerReader Source => track.Source;

        /// <summary>
        /// Decoder based on the <see cref="Codec"/> of the selected stream.
        /// </summary>
        Decoder decoder;

        /// <summary>
        /// If this track reader was created without keeping the reference to the container,
        /// the container is disposed with this track.
        /// </summary>
        readonly bool disposeSource;

        /// <summary>
        /// The referenced track from a container.
        /// </summary>
        readonly Track track;

        /// <summary>
        /// Reads an audio track from a container.
        /// </summary>
        public AudioTrackReader(Track track) : base(track.Source.reader) => this.track = track;

        /// <summary>
        /// Reads an audio track from a container and disposes the container after the reading was done.
        /// </summary>
        internal AudioTrackReader(Track track, bool disposeSource) : this(track) {
            this.disposeSource = disposeSource;
        }

        /// <summary>
        /// Fill the file metadata from the selected track.
        /// </summary>
        public override void ReadHeader() {
            TrackExtraAudio info = track.Extra as TrackExtraAudio;
            ChannelCount = info.ChannelCount;
            Length = (long)(info.SampleRate * track.Source.Duration);
            SampleRate = (int)info.SampleRate;
            Bits = info.Bits;

            switch (track.Format) {
                case Codec.AC3:
                case Codec.EnhancedAC3:
                    decoder = new EnhancedAC3Decoder(new BlockBuffer<byte>(track.ReadNextBlock));
                    break;
                case Codec.PCM_LE:
                case Codec.PCM_Float:
                    decoder = new RIFFWaveDecoder(new BlockBuffer<byte>(track.ReadNextBlock),
                        ChannelCount, Length, SampleRate, Bits);
                    break;
                default:
                    decoder = new DummyDecoder(track.Format, ChannelCount, Length, SampleRate);
                    break;
            }
        }

        /// <summary>
        /// If the stream can be rendered in 3D by Cavern, return a renderer.
        /// </summary>
        public override Renderer GetRenderer() {
            if (decoder == null) {
                ReadHeader();
            }
            if (decoder is RIFFWaveDecoder wav) {
                return new RIFFWaveRenderer(wav);
            }
            if (decoder is EnhancedAC3Decoder eac3) {
                return new EnhancedAC3Renderer(eac3);
            }
            if (decoder is DummyDecoder dummy) {
                return new DummyRenderer(dummy);
            }
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
        /// Goes back to a state where the first sample can be read.
        /// </summary>
        public override void Reset() {
            Source.Seek(0);
            ReadHeader();
        }

        /// <summary>
        /// Gets the metadata for the underlying codec in a human-readable format.
        /// </summary>
        public ReadableMetadata GetMetadata() => decoder is IMetadataSupplier meta ? meta.GetMetadata() : null;

        /// <summary>
        /// Close the reader if it surely can't be used anywhere else.
        /// </summary>
        public override void Dispose() {
            if (disposeSource && reader != null) {
                reader.Close();
            }
        }

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        /// <remarks>Seeking is not thread-safe.</remarks>
        public override void Seek(long sample) => throw new StreamingException();
    }
}