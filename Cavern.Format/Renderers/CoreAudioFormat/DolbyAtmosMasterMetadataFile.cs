using System.Collections.Generic;
using System.Numerics;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Renderers.CoreAudioFormat {
    /// <summary>
    /// Parses the .metadata file of a Dolby Atmos Master Format export to internal data representations.
    /// </summary>
    internal class DolbyAtmosMasterMetadataFile {
        /// <summary>
        /// Movement data for each dynamic object (bed channel object indices are null).
        /// </summary>
        public MovementTimeframe[][] Movement { get; }

        /// <summary>
        /// Parses the .metadata file of a Dolby Atmos Master Format export to internal data representations.
        /// </summary>
        /// <param name="source">Pre-parsed .metadata file</param>
        /// <param name="mapping">Maps PCM stream indices from the .audio file to internal object ID (those are the values)</param>
        public DolbyAtmosMasterMetadataFile(YAML source, int[] mapping) {
            Dictionary<int, int> inverseMapping = InvertMapping(mapping);
            List<MovementTimeframe>[] movement = new List<MovementTimeframe>[mapping.Length];
            MovementTimeframe[] lastFrames = new MovementTimeframe[mapping.Length];
            for (int i = 0; i < mapping.Length; i++) {
                movement[i] = new List<MovementTimeframe>();
            }

            if (!(source.Data.TryGetValue("events", out object rawEvents) &&
                rawEvents is List<YAMLObject> events)) {
                throw new CorruptionException("No events found in the metadata file.");
            }

            int id = -1;
            long offset = 0;
            if (!events[0].ContainsKey("ID")) {
                throw new CorruptionException("First event has no channel ID.");
            }

            for (int i = 0, c = events.Count; i < c; i++) {
                YAMLObject current = events[i];
                if (current.TryGetValue("ID", out object rawID)) {
                    if (!(rawID is string idString) || !int.TryParse(idString, out id)) {
                        throw new CorruptionException("Invalid channel ID in the metadata file.");
                    }
                }
                id = inverseMapping[id];

                if (current.TryGetValue("samplePos", out object rawOffset) &&
                    rawOffset is string offsetSource) {
                    offset = long.Parse(offsetSource);
                }

                Vector3 position = lastFrames[id].position;
                if (current.TryGetValue("pos", out object rawPosition) &&
                    rawPosition is string positionSource) {
                    string[] parts = positionSource[1..^1].Split(", ");
                    position = new Vector3(QMath.ParseFloat(parts[0]), QMath.ParseFloat(parts[2]), QMath.ParseFloat(parts[1]));
                }

                float gain = lastFrames[id].gain;
                if (current.TryGetValue("gain", out object rawGain) &&
                    rawGain is string gainSource) {
                    gain = gainSource != "-inf" ? QMath.DbToGain(QMath.ParseFloat(gainSource)) : 0;
                }

                int fade = lastFrames[id].fade;
                if (current.TryGetValue("rampLength", out object rawFade) &&
                    rawFade is string fadeSource) {
                    fade = int.Parse(fadeSource);
                }

                lastFrames[id] = new MovementTimeframe(position, gain, offset, fade);
                movement[id].Add(lastFrames[id]);
            }

            Movement = new MovementTimeframe[movement.Length][];
            for (int i = 0; i < movement.Length; i++) {
                Movement[i] = movement[i].Count != 0 ? movement[i].ToArray() : null;
            }
        }

        /// <summary>
        /// Create a dictionary that maps the internal object IDs to the PCM stream indices of the .audio file.
        /// </summary>
        static Dictionary<int, int> InvertMapping(int[] mapping) {
            Dictionary<int, int> result = new Dictionary<int, int>();
            for (int i = 0; i < mapping.Length; i++) {
                result[mapping[i]] = i;
            }
            return result;
        }
    }
}
