using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Renderers;
using Cavern.Remapping;
using System;
using System.Text;

namespace CavernizeGUI {
    /// <summary>
    /// Represents an audio track of an audio file.
    /// </summary>
    class Track : IDisposable {
        /// <summary>
        /// This track can be rendered.
        /// </summary>
        public bool Supported { get; }

        /// <summary>
        /// Text to display about this track.
        /// </summary>
        public string Details { get; private set; }

        /// <summary>
        /// Source of audio data.
        /// </summary>
        readonly AudioReader reader;

        /// <summary>
        /// Track audio coding type.
        /// </summary>
        readonly Codec codec;

        /// <summary>
        /// Language code.
        /// </summary>
        readonly string language;

        /// <summary>
        /// Reads information from a track's renderer.
        /// </summary>
        public Track(AudioReader reader, Codec codec, string language = null) {
            this.reader = reader;
            this.codec = codec;
            this.language = language;

            StringBuilder builder = new();
            Renderer renderer = reader.GetRenderer();
            Supported = renderer != null;
            if (!Supported)
                builder.AppendLine("Format unsupported by Cavern");
            else if (renderer is EnhancedAC3Renderer eac3)
                if (eac3.HasObjects)
                    builder.AppendLine("Enhanced AC-3 with Joint Object Coding")
                        .Append("Number of bed channels: ").AppendLine((eac3.Objects.Count - eac3.DynamicObjects).ToString())
                        .Append("Number of dynamic objects: ").AppendLine(eac3.DynamicObjects.ToString());
                else
                    builder.AppendLine("Enhanced AC-3");
            else
                builder.AppendLine("Channel-based audio track");

            ReferenceChannel[] beds = renderer != null ? renderer.GetChannels() : Array.Empty<ReferenceChannel>();
            if (beds.Length > 0)
                builder.Append("Source channels (").Append(reader.ChannelCount).Append("): ")
                    .AppendLine(string.Join(", ", beds));
            else
                builder.Append("Source stream count: ").AppendLine(reader.ChannelCount.ToString());
            builder.Append("Length: ").AppendLine(TimeSpan.FromSeconds(reader.Length / (double)reader.SampleRate).ToString());
            builder.Append("Sample rate: ").Append(reader.SampleRate).AppendLine("Hz");
            Details = builder.ToString();
        }

        /// <summary>
        /// Free up resources.
        /// </summary>
        public void Dispose() => reader.Dispose();

        /// <summary>
        /// Very short track information for the dropdown.
        /// </summary>
        public override string ToString() => string.IsNullOrEmpty(language) ? codec.ToString() : $"{codec} ({language})";
    }
}