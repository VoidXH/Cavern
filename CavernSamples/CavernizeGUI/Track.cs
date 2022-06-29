using Cavern;
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
        /// The renderer starting at the first sample of the track after construction.
        /// </summary>
        public Renderer Renderer { get; }

        /// <summary>
        /// Number of samples to render by the <see cref="Listener"/> to reach the end of the stream.
        /// </summary>
        public long Length => reader.Length;

        /// <summary>
        /// Sampling rate of the track.
        /// </summary>
        public int SampleRate => reader.SampleRate;

        /// <summary>
        /// Order in the tracks of the input container.
        /// </summary>
        public int Index { get; }

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
        public Track(AudioReader reader, Codec codec, int index, string language = null) {
            this.reader = reader;
            this.codec = codec;
            Index = index;
            this.language = language;

            StringBuilder builder = new();
            Renderer = reader.GetRenderer();
            Supported = Renderer != null;
            if (!Supported)
                builder.AppendLine("Format unsupported by Cavern");
            else if (Renderer is EnhancedAC3Renderer eac3)
                if (eac3.HasObjects)
                    builder.AppendLine("Enhanced AC-3 with Joint Object Coding")
                        .Append("Number of bed channels: ").AppendLine((eac3.Objects.Count - eac3.DynamicObjects).ToString())
                        .Append("Number of dynamic objects: ").AppendLine(eac3.DynamicObjects.ToString());
                else
                    builder.AppendLine("Enhanced AC-3");
            else
                builder.AppendLine("Channel-based audio track");

            ReferenceChannel[] beds = Renderer != null ? Renderer.GetChannels() : Array.Empty<ReferenceChannel>();
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
        /// Attach this track to a rendering environment.
        /// </summary>
        public void Attach(Listener listener) {
            listener.SampleRate = reader.SampleRate;
            for (int i = 0; i < Renderer.Objects.Count; ++i)
                listener.AttachSource(Renderer.Objects[i]);
        }

        /// <summary>TODO: THIS IS TEMPORARY, REMOVE WHEN AC3 IS DECODABLE</summary>
        public void SetRendererSource(AudioReader reader) {
            if (Renderer is EnhancedAC3Renderer eac3)
                eac3.Source = reader;
        }

        /// <summary>TODO: THIS IS TEMPORARY, REMOVE WHEN AC3 IS DECODABLE</summary>
        public void SetupForExport() {
            if (Renderer is EnhancedAC3Renderer eac3 && eac3.Source != null)
                eac3.Source.Reset();
        }

        /// <summary>TODO: THIS IS TEMPORARY, REMOVE WHEN AC3 IS DECODABLE</summary>
        public void DisposeRendererSource() {
            if (Renderer is EnhancedAC3Renderer eac3 && eac3.Source != null)
                eac3.Source.Dispose();
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