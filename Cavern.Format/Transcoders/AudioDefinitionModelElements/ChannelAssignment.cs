using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Connects RIFF WAVE channels with <see cref="AudioDefinitionModel"/> tracks.
    /// </summary>
    public sealed class ChannelAssignment {
        /// <summary>
        /// The parsed channel assignment.
        /// </summary>
        public Tuple<short, string>[] Assignment { get; private set; }

        /// <summary>
        /// Read the channel assignment from an ADM BWF file's related chunk.
        /// </summary>
        public ChannelAssignment(Stream reader) {
            short count = reader.ReadInt16();
            reader.Position += 2; // Count again

            Assignment = new Tuple<short, string>[count];
            for (short i = 0; i < count; i++) {
                Assignment[i] = new Tuple<short, string>(reader.ReadInt16(), reader.ReadCString());
            }
        }

        /// <summary>
        /// Get the bytes of a channel assignment chunk for an ADM.
        /// </summary>
        public static byte[] GetChunk(AudioDefinitionModel adm) {
            List<byte> result = new List<byte>();
            ushort count = (ushort)adm.Movements.Count;
            byte[] size = BitConverter.GetBytes(count);
            result.AddRange(size);
            result.AddRange(size);
            for (ushort i = 0; i < count;) {
                ADMStreamFormat stream = FindStream(adm, adm.Movements[i]);
                result.AddRange(BitConverter.GetBytes(++i));
                result.AddRange(Encoding.ASCII.GetBytes(FindTrack(adm.Tracks, stream.TrackFormat).ID));
                result.AddRange(Encoding.ASCII.GetBytes(stream.TrackFormat));
                result.AddRange(Encoding.ASCII.GetBytes(stream.PackFormat));
                result.Add(0);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Find a stream by movement information.
        /// </summary>
        static ADMStreamFormat FindStream(AudioDefinitionModel adm, ADMChannelFormat target) {
            for (int i = 0, c = adm.StreamFormats.Count; i < c; i++) {
                for (int j = 0, c2 = adm.ChannelFormats.Count; j < c2; j++) {
                    if (adm.StreamFormats[i].ChannelFormat.Equals(target.ID) && adm.ChannelFormats[j].ID.Equals(target.ID)) {
                        return adm.StreamFormats[i];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Find the track that contains a track format.
        /// </summary>
        static ADMTrack FindTrack(IReadOnlyList<ADMTrack> tracks, string trackFormat) {
            for (int i = 0, c = tracks.Count; i < c; i++) {
                if (tracks[i].TrackFormat.Equals(trackFormat)) {
                    return tracks[i];
                }
            }
            return null;
        }
    }
}