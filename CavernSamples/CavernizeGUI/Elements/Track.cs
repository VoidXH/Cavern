using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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
    public class Track : IDisposable, IMetadataSupplier {
        /// <summary>
        /// Expanded names of codecs for which the enum is a shorthand.
        /// </summary>
        static readonly Dictionary<Codec, string> formatNames = new() {
            [Codec.DTS] = "DTS Coherent Acoustics",
            [Codec.DTS_HD] = "DTS-HD",
            [Codec.AC3] = "AC-3",
            [Codec.EnhancedAC3] = "Enhanced AC-3",
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
        /// The track's container's file format.
        /// </summary>
        public Container Container => reader is AudioTrackReader trackReader ? trackReader.Source.Type : Container.NotContainer;

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
        /// Major format information to display first when this track is selected.
        /// </summary>
        public string FormatHeader { get; protected set; }

        /// <summary>
        /// Main properties of this audio track.
        /// </summary>
        public (string property, string value)[] Details { get; protected set; }

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

            Renderer = reader.GetRenderer();
            Supported = Renderer is not DummyRenderer;
            EnhancedAC3Renderer eac3 = Renderer as EnhancedAC3Renderer;

            ResourceDictionary strings = Consts.Language.GetTrackStrings();
            if (!Supported) {
                FormatHeader = (string)strings["NoSup"];
            } else if (eac3 != null) {
                if (eac3.HasObjects) {
                    FormatHeader = (string)strings["E3JOC"];
                } else {
                    FormatHeader = eac3.Enhanced ? "Enhanced AC-3" : "AC-3";
                }
            } else {
                FormatHeader = Renderer.HasObjects ? (string)strings["ObTra"] : (string)strings["ChTra"];
            }

            ReferenceChannel[] beds = Renderer != null ? Renderer.GetChannels() : Array.Empty<ReferenceChannel>();
            string bedList = string.Join(' ', ChannelPrototype.GetShortNames(beds));
            List<(string, string)> builder = new();
            if (eac3 != null && eac3.HasObjects) {
                builder.Add(((string)strings["SouCh"], $"{beds.Length} - {bedList}"));
                string[] newBeds = ChannelPrototype.GetShortNames(eac3.GetStaticChannels());
                builder.Add(((string)strings["MatBe"], $"{newBeds.Length} - {string.Join(' ', newBeds)}"));
                builder.Add(((string)strings["MatOb"], eac3.DynamicObjects.ToString()));
            } else {
                if (Renderer != null && beds.Length != Renderer.Objects.Count) {
                    if (beds.Length > 0) {
                        builder.Add(((string)strings["SouBe"], $"{beds.Length} - {bedList}"));
                    }
                    builder.Add(((string)strings["SouDy"], (Renderer.Objects.Count - beds.Length).ToString()));
                } else if (beds.Length > 0) {
                    builder.Add(((string)strings["Chans"], $"{beds.Length} - {bedList}"));
                } else {
                    builder.Add(((string)strings["ChCnt"], reader.ChannelCount.ToString()));
                }
            }
            builder.Add(((string)strings["TraLe"], TimeSpan.FromSeconds(reader.Length / (double)reader.SampleRate).ToString()));
            builder.Add(((string)strings["TraFs"], reader.SampleRate + " Hz"));
            Details = builder.ToArray();
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
        /// Get the video tracks this audio track is accompanying.
        /// </summary>
        public Cavern.Format.Common.Track[] GetVideoTracks() =>
            reader is AudioTrackReader trackReader ? trackReader.Source.Tracks.Where(x => x.Format.IsVideo()).ToArray() : null;

        /// <summary>
        /// Gets the metadata for this codec in a human-readable format.
        /// </summary>
        public ReadableMetadata GetMetadata() => reader is IMetadataSupplier meta ? meta.GetMetadata() : null;

        /// <summary>
        /// Free up resources.
        /// </summary>
        public void Dispose() {
            if (reader is AudioTrackReader track) {
                track.Source.Dispose();
            }
            reader?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Very short track information for the dropdown.
        /// </summary>
        public override string ToString() {
            ResourceDictionary strings = Consts.Language.GetTrackStrings();
            string codecName = (string)strings[Codec.ToString()] ??
                (formatNames.ContainsKey(Codec) ? formatNames[Codec] : Codec.ToString());
            string objects = Renderer != null && Renderer.HasObjects ? " " + strings["WiObj"] : string.Empty;
            return string.IsNullOrEmpty(Language) ? codecName : $"{codecName}{objects} ({Language})";
        }
    }
}