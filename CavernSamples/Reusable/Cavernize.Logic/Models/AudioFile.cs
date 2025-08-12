using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Container;

using Cavernize.Logic.Language;

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
    /// Track handlers and info providers.
    /// </summary>
    public IReadOnlyList<CavernizeTrack> Tracks => tracks;

    /// <summary>
    /// List of track handlers.
    /// </summary>
    protected readonly List<CavernizeTrack> tracks = [];

    /// <summary>
    /// Localization of user-visible strings.
    /// </summary>
    protected readonly TrackStrings language;

    /// <summary>
    /// Loads an audio file.
    /// </summary>
    public AudioFile(string path, TrackStrings language) {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        this.language = language ?? throw new ArgumentNullException(nameof(language));
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
                tracks.Add(new CavernizeTrack(new EnhancedAC3Reader(Path), Codec.EnhancedAC3, 0, language));
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
    /// Get the index of a track that is the same as the selected index, but in better quality.
    /// This feature can use a better codec's waveform with a worse but known codec's spatial metadata.
    /// </summary>
    public int TryForBetterQuality(CavernizeTrack than) {
        if (than.Codec == Codec.EnhancedAC3 && than.Renderer.HasObjects) {
            for (int i = 0, c = tracks.Count; i < c; i++) {
                CavernizeTrack target = tracks[i];
                if (target.Codec == Codec.TrueHD && than.Index - 2 <= target.Index && target.Index < than.Index &&
                    than.Language.Equals(target.Language) && than.Renderer.Channels == target.Renderer.Channels) {
                    return target.Index;
                }
            }
        }
        return than.Index;
    }

    /// <summary>
    /// Free up resources.
    /// </summary>
    public void Dispose() {
        for (int i = 0; i < tracks.Count; ++i) {
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
    void AddStandaloneTrack(AudioReader reader, Codec codec) => tracks.Add(new CavernizeTrack(reader, codec, 0, language));

    /// <summary>
    /// Add the tracks of a container to the track list.
    /// </summary>
    void AddTracksFromContainer(ContainerReader reader) {
        int trackId = 0;
        for (int i = 0; i < reader.Tracks.Length; i++) {
            if (reader.Tracks[i].Extra is TrackExtraAudio) {
#if RELEASE
                try {
#endif
                    tracks.Add(new CavernizeTrack(new AudioTrackReader(reader.Tracks[i]), reader.Tracks[i].Format,
                        trackId, language, reader.Tracks[i].Language));
#if RELEASE
                } catch (Exception e) {
                    tracks.Add(new InvalidTrack(e.Message, reader.Tracks[i].Format, reader.Tracks[i].Language, language));
                }
#endif
                trackId++;
            }
        }
    }
}
