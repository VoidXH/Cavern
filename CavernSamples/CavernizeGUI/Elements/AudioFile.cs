using System;
using System.Collections.Generic;

using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Container;

namespace CavernizeGUI.Elements {
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
        public IReadOnlyList<Track> Tracks => tracks;

        /// <summary>
        /// List of track handlers.
        /// </summary>
        protected readonly List<Track> tracks = new List<Track>();

        /// <summary>
        /// Loads an audio file.
        /// </summary>
        public AudioFile(string path) {
            Path = path;
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
                    tracks.Add(new Track(new EnhancedAC3Reader(Path), Codec.EnhancedAC3, 0));
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
                case "wav":
                    RIFFWaveReader wavReader = new(Path);
                    tracks.Add(new Track(wavReader, wavReader.Bits == BitDepth.Float32 ? Codec.PCM_Float : Codec.PCM_LE, 0));
                    break;
                case "laf":
                    LimitlessAudioFormatReader lafReader = new(Path);
                    tracks.Add(new Track(lafReader, lafReader.Bits == BitDepth.Float32 ? Codec.PCM_Float : Codec.PCM_LE, 0));
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Get the index of a track that is the same as the selected index, but in better quality.
        /// This feature can use a better codec's waveform with a worse but known codec's spatial metadata.
        /// </summary>
        public int TryForBetterQuality(Track than) {
            if (than.Codec == Codec.EnhancedAC3 && than.Renderer.HasObjects) {
                for (int i = 0, c = tracks.Count; i < c; i++) {
                    Track target = tracks[i];
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
        /// Add the tracks of a container to the track list.
        /// </summary>
        void AddTracksFromContainer(ContainerReader reader) {
            int trackId = 0;
            for (int i = 0; i < reader.Tracks.Length; i++) {
                if (reader.Tracks[i].Extra is TrackExtraAudio) {
                    try {
                        tracks.Add(new Track(new AudioTrackReader(reader.Tracks[i]), reader.Tracks[i].Format,
                            trackId, reader.Tracks[i].Language));
                    } catch (Exception e) {
                        tracks.Add(new InvalidTrack(e.Message, reader.Tracks[i].Format, reader.Tracks[i].Language));
                    }
                    ++trackId;
                }
            }
        }
    }
}