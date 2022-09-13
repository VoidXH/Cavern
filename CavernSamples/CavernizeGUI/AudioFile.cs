﻿using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Container;
using System;
using System.Collections.Generic;

namespace CavernizeGUI {
    /// <summary>
    /// Audio file parser and tracklist provider.
    /// </summary>
    class AudioFile : IDisposable {
        /// <summary>
        /// Track handlers and info providers.
        /// </summary>
        public IReadOnlyList<Track> Tracks => tracks;

        /// <summary>
        /// List of track handlers.
        /// </summary>
        readonly List<Track> tracks = new List<Track>();

        /// <summary>
        /// Path to the file.
        /// </summary>
        readonly string path;

        /// <summary>
        /// Loads an audio file.
        /// </summary>
        public AudioFile(string path) {
            this.path = path;
            Reset();
        }

        /// <summary>
        /// Reloads the tracklist to be able to start reading from the beginning.
        /// </summary>
        public void Reset() {
            Dispose();
            tracks.Clear();
            switch (path[^3..]) {
                case "ac3":
                case "ec3":
                    EnhancedAC3Reader ac3Reader = new(path);
                    tracks.Add(new Track(ac3Reader, Codec.EnhancedAC3, 0));
                    break;
                case "mkv":
                case "mka":
                    MatroskaReader mkvReader = new(path);
                    int trackId = 0;
                    for (int i = 0; i < mkvReader.Tracks.Length; ++i)
                        if (mkvReader.Tracks[i].Format.IsAudio())
                            tracks.Add(new Track(new AudioTrackReader(mkvReader.Tracks[i]), mkvReader.Tracks[i].Format,
                                trackId++, mkvReader.Tracks[i].Language));
                    break;
                case "wav":
                    RIFFWaveReader wavReader = new(path);
                    tracks.Add(new Track(wavReader, wavReader.Bits == BitDepth.Float32 ? Codec.PCM_Float : Codec.PCM_LE, 0));
                    break;
                case "laf":
                    throw new NotSupportedException();
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
            for (int i = 0; i < tracks.Count; ++i)
                tracks[i].Dispose();
        }
    }
}