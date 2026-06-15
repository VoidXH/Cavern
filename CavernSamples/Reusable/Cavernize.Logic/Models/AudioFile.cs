using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Common.Metadata;
using Cavern.Format.Container;

using Cavernize.Logic.Exceptions;

namespace Cavernize.Logic.Models;

/// <summary>
/// Audio file parser and tracklist provider.
/// </summary>
public class AudioFile : IDisposable {
    /// <summary>
    /// Path to the file.
    /// </summary>
    public string Path { get; private set; }

    /// <summary>
    /// Track handlers and info providers for all audio tracks, including unsupported.
    /// </summary>
    public IReadOnlyList<CavernizeTrack> Tracks => tracks;

    /// <summary>
    /// All native tracks of the file, including video and subtitle tracks.
    /// </summary>
    public IReadOnlyList<Track> AllTracks => tracks.FirstOrDefault(x => x.Supported)?.Track.Source.Tracks ?? throw new NoSupportedTracksException();

    /// <summary>
    /// List of track handlers.
    /// </summary>
    protected readonly List<CavernizeTrack> tracks = [];

    /// <summary>
    /// Loads an audio file.
    /// </summary>
    public AudioFile(string path) {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Reset();
    }

    /// <summary>
    /// Reloads the tracklist to be able to start reading from the beginning.
    /// </summary>
    public virtual void Reset() {
        Dispose();
        tracks.Clear();
        int index = Path.LastIndexOf('.') + 1;
        if (index == 0 || index == Path.Length) {
            throw new NotSupportedException();
        }
        switch (Path[index..]) {
            case "ac3":
            case "eac3":
            case "ec3":
                tracks.Add(new CavernizeTrack(new EnhancedAC3Reader(Path), Codec.EnhancedAC3, 0));
                break;
            case "mkv":
            case "mka":
            case "webm":
            case "weba":
                AddTracksFromContainer(new MatroskaReader(Path));
                break;
            case "m4a":
            case "m4v":
            case "mov":
            case "mp4":
            case "qt":
                AddTracksFromContainer(new MP4Reader(Path));
                break;
            case "mxf":
                AddTracksFromContainer(new MXFReader(Path));
                break;
            case "atmos":
                AddStandaloneTrack(new DolbyAtmosMasterFormatReader(Path), Codec.DAMF);
                break;
            case "caf":
                AddStandaloneTrack(new CoreAudioFormatReader(Path));
                break;
            case "mlp":
            case "thd":
            case "truehd":
                AddStandaloneTrack(new MeridianLosslessPackingReader(Path), Codec.TrueHD);
                break;
            case "wav":
                AddStandaloneTrack(new RIFFWaveReader(Path));
                break;
            case "laf":
                AddStandaloneTrack(new LimitlessAudioFormatReader(Path));
                break;
            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Free up resources.
    /// </summary>
    public void Dispose() {
        for (int i = 0; i < tracks.Count; i++) {
            tracks[i].Dispose();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Show the file's name.
    /// </summary>
    public override string ToString() => System.IO.Path.GetFileName(Path);

    /// <summary>
    /// Add a track from a file that contains the raw bitstream of a single audio track.
    /// </summary>
    void AddStandaloneTrack(AudioReader reader) => AddStandaloneTrack(reader, reader.Bits == BitDepth.Float32 ? Codec.PCM_Float : Codec.PCM_LE);

    /// <summary>
    /// Add a track from a file that contains the raw bitstream of a single audio track.
    /// </summary>
    void AddStandaloneTrack(AudioReader reader, Codec codec) => tracks.Add(new CavernizeTrack(reader, codec, 0));

    /// <summary>
    /// Add the tracks of a container to the track list.
    /// </summary>
    void AddTracksFromContainer(ContainerReader reader) {
        int trackId = 0;
        for (int i = 0; i < reader.Tracks.Length; i++) {
            if (reader.Tracks[i].Extra is TrackExtraAudio) {
                try {
                    tracks.Add(new CavernizeTrack(new AudioTrackReader(reader.Tracks[i]), reader.Tracks[i].Format,
                        trackId, reader.Tracks[i].Language));
                } catch (Exception e) {
                    tracks.Add(new InvalidTrack(e.Message, reader.Tracks[i].Format, reader.Tracks[i].Language));
                }
                trackId++;
            }
        }
    }
}
