using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Cavern;
using Cavern.Channels;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Renderers;
using Cavern.Remapping;
using Cavern.Utilities;
using CavernizeGUI.Resources;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// Represents an audio track of an audio file.
    /// </summary>
    public class Track : IDisposable {
        readonly static Dictionary<Codec, string> formatNames = new() {
            [Codec.DTS] = "DTS Coherent Acoustics",
            [Codec.DTS_HD] = "DTS-HD",
            [Codec.EnhancedAC3] = "Enhanced AC-3",
            [Codec.PCM_Float] = "PCM (floating point)",
            [Codec.PCM_LE] = "PCM (integer)",
        };

        /// <summary>
        /// This track can be rendered.
        /// </summary>
        public bool Supported { get; }

        /// <summary>
        /// Track audio coding type.
        /// </summary>
        public Codec Codec { get; protected set; }

        /// <summary>
        /// The renderer starting at the first sample of the track after construction.
        /// </summary>
        public Renderer Renderer { get; private set; }

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
        /// Language code.
        /// </summary>
        public string Language { get; protected set; }

        /// <summary>
        /// Text to display about this track.
        /// </summary>
        public string Details { get; protected set; }

        /// <summary>
        /// Source of audio data.
        /// </summary>
        readonly AudioReader reader;

        /// <summary>
        /// Reads information from a track's renderer.
        /// </summary>
        public Track(AudioReader reader, Codec codec, int index) {
            this.reader = reader;
            Codec = codec;
            Index = index;

            StringBuilder builder = new();
            Renderer = reader.GetRenderer();
            Supported = Renderer is not DummyRenderer;
            EnhancedAC3Renderer eac3 = Renderer as EnhancedAC3Renderer;

            if (!Supported) {
                builder.AppendLine("Format unsupported by Cavern");
            } else if (eac3 != null) {
                if (eac3.HasObjects) {
                    builder.AppendLine("Enhanced AC-3 with Joint Object Coding");
                } else {
                    builder.AppendLine(eac3.Enhanced ? "Enhanced AC-3" : "AC-3");
                }
            } else {
                if (Renderer.HasObjects) {
                    builder.Append("Object");
                } else {
                    builder.Append("Channel");
                }
                builder.AppendLine("-based audio track");
            }

            ReferenceChannel[] beds = Renderer != null ? Renderer.GetChannels() : Array.Empty<ReferenceChannel>();
            string bedList = string.Join(", ", ChannelPrototype.GetNames(beds));
            if (eac3 != null && eac3.HasObjects) {
                builder.Append("Source channels (").Append(reader.ChannelCount).Append("): ").AppendLine(bedList)
                    .Append("Matrixed bed channels: ").AppendLine((eac3.Objects.Count - eac3.DynamicObjects).ToString())
                    .Append("Matrixed dynamic objects: ").AppendLine(eac3.DynamicObjects.ToString());
            } else {
                if (Renderer != null && beds.Length != Renderer.Objects.Count) {
                    if (beds.Length > 0) {
                        builder.Append("Bed channels (").Append(beds.Length).Append("): ").AppendLine(bedList);
                    }
                    builder.Append("Dynamic objects: ").AppendLine((Renderer.Objects.Count - beds.Length).ToString());
                } else if (beds.Length > 0) {
                    builder.Append("Channels (").Append(beds.Length).Append("): ").AppendLine(bedList);
                } else {
                    builder.Append("Channel count: ").AppendLine(reader.ChannelCount.ToString());
                }
            }

            builder.Append("Length: ").AppendLine(TimeSpan.FromSeconds(reader.Length / (double)reader.SampleRate).ToString());
            builder.Append("Sample rate: ").Append(reader.SampleRate).AppendLine("Hz");
            Details = builder.ToString();
        }

        /// <summary>
        /// Reads information from a track's renderer.
        /// </summary>
        public Track(AudioReader reader, Codec codec, int index, string language) : this(reader, codec, index) => Language = language;

        /// <summary>
        /// Empty constructor for derived classes.
        /// </summary>
        protected Track() { }

        /// <summary>
        /// Attach this track to a rendering environment and start from the beginning.
        /// </summary>
        public void Attach(Listener listener) {
            reader.Reset();
            Renderer = reader.GetRenderer();
            listener.SampleRate = SampleRate;

            Source[] attachables;

            if (UpmixingSettings.Default.MatrixUpmix && !Renderer.HasObjects) {
                ReferenceChannel[] channels = Renderer.GetChannels();
                SurroundUpmixer upmixer = new SurroundUpmixer(channels, SampleRate, false, true);
                RunningChannelSeparator separator = new RunningChannelSeparator(channels.Length) {
                    GetSamples = input => reader.ReadBlock(input, 0, input.Length)
                };
                upmixer.OnSamplesNeeded += updateRate => separator.Update(updateRate);

                listener.LFESeparation = channels.Contains(ReferenceChannel.ScreenLFE); // Apply crossover if LFE is not present
                attachables = upmixer.IntermediateSources;
            } else {
                listener.LFESeparation = true;
                attachables = Renderer.Objects.ToArray();
            }

            if (UpmixingSettings.Default.Cavernize && !Renderer.HasObjects) {
                CavernizeUpmixer cavernizer = new CavernizeUpmixer(attachables, SampleRate) {
                    Effect = UpmixingSettings.Default.Effect,
                    Smoothness = UpmixingSettings.Default.Smoothness
                };
                attachables = cavernizer.IntermediateSources;
            }

            for (int i = 0; i < attachables.Length; i++) {
                listener.AttachSource(attachables[i]);
            }
        }

        /// <summary>
        /// Free up resources.
        /// </summary>
        public void Dispose() {
            if (reader is AudioTrackReader track) {
                track.Source.Dispose();
            }
            reader.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Very short track information for the dropdown.
        /// </summary>
        public override string ToString() {
            string codecName = formatNames.ContainsKey(Codec) ? formatNames[Codec] : Codec.ToString();
            string objects = Renderer != null && Renderer.HasObjects ? " with objects" : string.Empty;
            return string.IsNullOrEmpty(Language) ? codecName : $"{codecName}{objects} ({Language})";
        }
    }
}