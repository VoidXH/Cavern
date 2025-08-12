using Cavern;
using Cavern.CavernSettings;
using Cavern.Channels;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Renderers;
using Cavern.Remapping;
using Cavern.Utilities;

using Cavernize.Logic.Language;

namespace Cavernize.Logic.Models;

/// <summary>
/// Represents an audio track of the loaded audio file.
/// </summary>
public class CavernizeTrack : IDisposable, IMetadataSupplier {
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
    /// The file in which this track is stored, or null if it's a network stream.
    /// </summary>
    public string Path => reader.Path;

    /// <summary>
    /// Raw access to the track's stream.
    /// </summary>
    public Track Track => reader is AudioTrackReader trackReader ? trackReader.track : null;

    /// <summary>
    /// Source of audio data.
    /// </summary>
    readonly AudioReader reader;

    /// <summary>
    /// Localized strings for track description.
    /// </summary>
    readonly TrackStrings strings;

    /// <summary>
    /// Reads information from a track's renderer.
    /// </summary>
    public CavernizeTrack(AudioReader reader, Codec codec, int index, TrackStrings strings) {
        this.reader = reader;
        Codec = codec;
        Index = index;
        this.strings = strings;

        Renderer = reader.GetRenderer();
        Supported = Renderer is not DummyRenderer;
        EnhancedAC3Renderer eac3 = Renderer as EnhancedAC3Renderer;

        if (!Supported) {
            FormatHeader = strings.NotSupported;
        } else if (eac3 != null) {
            if (eac3.HasObjects) {
                FormatHeader = strings.TypeEAC3JOC;
            } else {
                FormatHeader = eac3.Enhanced ? "Enhanced AC-3" : "AC-3";
            }
        } else {
            FormatHeader = Renderer.HasObjects ? strings.ObjectBasedTrack : strings.ChannelBasedTrack;
        }
        FormatHeader += $" ({TimeSpan.FromSeconds(reader.Length / (double)reader.SampleRate):h\\:mm\\:ss})";

        ReferenceChannel[] beds = Renderer != null ? Renderer.GetChannels() : [];
        string bedList = string.Join(' ', ChannelPrototype.GetShortNames(beds));
        List<(string, string)> builder = [];
        if (eac3 != null && eac3.HasObjects) {
            builder.Add((strings.SourceChannels, $"{beds.Length} - {bedList}"));
            string[] newBeds = ChannelPrototype.GetShortNames(eac3.GetStaticChannels());
            builder.Add((strings.MatrixedBeds, $"{newBeds.Length} - {string.Join(' ', newBeds)}"));
            builder.Add((strings.MatrixedObjects, eac3.DynamicObjects.ToString()));
        } else {
            if (Renderer != null && beds.Length != Renderer.Objects.Count) { // Generic object-based format: bed and object lines
                if (beds.Length > 0) {
                    builder.Add((strings.BedChannels, $"{beds.Length} - {bedList}"));
                }
                builder.Add((strings.DynamicObjects, (Renderer.Objects.Count - beds.Length).ToString()));
            } else if (beds.Length > 0) { // Generic channel-based format of known channels
                builder.Add((strings.Channels, $"{beds.Length} - {bedList}"));
            } else { // Generic channel-based format of unknown channels
                builder.Add((strings.Channels, reader.ChannelCount.ToString()));
            }
        }
        Details = [.. builder];
    }

    /// <summary>
    /// Reads information from a track's renderer.
    /// </summary>
    public CavernizeTrack(AudioReader reader, Codec codec, int index, TrackStrings strings, string language) :
        this(reader, codec, index, strings) => Language = language;

    /// <summary>
    /// Language-only constructor for derived classes.
    /// </summary>
    protected CavernizeTrack(TrackStrings strings) => this.strings = strings;

    /// <summary>
    /// Attach this track to a rendering environment and start from the beginning.
    /// </summary>
    public void Attach(Listener listener, UpmixingSettings upmixingSettings) {
        reader.Reset();
        Renderer = reader.GetRenderer();
        listener.SampleRate = SampleRate;

        Source[] attachables;

        if (upmixingSettings.MatrixUpmixing && !Renderer.HasObjects) {
            ReferenceChannel[] channels = Renderer.GetChannels();
            SurroundUpmixer upmixer = new SurroundUpmixer(channels, SampleRate, false, true);
            RunningChannelSeparator separator = new RunningChannelSeparator(channels.Length) {
                GetSamples = input => reader.ReadBlock(input, 0, input.Length)
            };
            upmixer.OnSamplesNeeded = separator.Update;

            listener.LFESeparation = channels.Contains(ReferenceChannel.ScreenLFE); // Apply crossover if LFE is not present
            attachables = upmixer.IntermediateSources;
        } else {
            listener.LFESeparation = true;
            attachables = [.. Renderer.Objects];
        }

        if (upmixingSettings.Cavernize && !Renderer.HasObjects) {
            CavernizeUpmixer cavernizer = new CavernizeUpmixer(attachables, SampleRate) {
                Effect = upmixingSettings.Effect,
                Smoothness = upmixingSettings.Smoothness
            };
            attachables = cavernizer.IntermediateSources;
        }

        listener.AttachSources(attachables);
    }

    /// <summary>
    /// Get the video tracks this audio track is accompanying.
    /// </summary>
    public Track[] GetVideoTracks() => reader is AudioTrackReader trackReader ? trackReader.Source.Tracks.Where(x => x.Format.IsVideo()).ToArray() : null;

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
        strings.CodecNames.TryGetValue(Codec, out string codecName);
        codecName ??= formatNames.TryGetValue(Codec, out string value) ? value : Codec.ToString();
        string objects = Renderer != null && Renderer.HasObjects ? " " + strings.WithObjects : string.Empty;
        return string.IsNullOrEmpty(Language) ? codecName : $"{codecName}{objects} ({Language})";
    }

    /// <summary>
    /// Expanded names of codecs for which the enum is a shorthand.
    /// </summary>
    static readonly Dictionary<Codec, string> formatNames = new() {
        [Codec.DAMF] = "Dolby Atmos Master Format",
        [Codec.DTS] = "DTS Coherent Acoustics",
        [Codec.DTS_HD] = "DTS-HD",
        [Codec.AC3] = "AC-3",
        [Codec.EnhancedAC3] = "Enhanced AC-3",
    };
}
