namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains tags that describe an ADM metadata.
    /// </summary>
    static class ADMTags {
        /// <summary>
        /// Root element of an ADM metadata XML.
        /// </summary>
        public const string rootTag = "ebuCoreMain";

        /// <summary>
        /// Namespace attribute of the root element.
        /// </summary>
        public const string rootNamespaceAttribute = "xmlns";

        /// <summary>
        /// The XML namespace of an ADM XML.
        /// </summary>
        public const string rootNamespace = "urn:ebu:metadata-schema:ebuCore_2016";

        /// <summary>
        /// Sub-namespace of the root element.
        /// </summary>
        public const string instanceNamespaceAttribute = "xsi";

        /// <summary>
        /// Value for the sub-namespace of the root element.
        /// </summary>
        public const string instanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";

        /// <summary>
        /// ADM XML schema location URI attribute.
        /// </summary>
        public const string schemaLocationAttribute = "schemaLocation";

        /// <summary>
        /// ADM XML schema location URI. Appended to <see cref="rootNamespace"/>.
        /// </summary>
        public const string schemaLocation = " ebucore.xsd";

        /// <summary>
        /// Attribute to set the language of the ADM XML.
        /// </summary>
        public const string languageAttribute = "lang";

        /// <summary>
        /// Language of the ADM XML.
        /// </summary>
        public const string language = "en";

        /// <summary>
        /// Required tags before actual content.
        /// </summary>
        public static readonly string[] subTags = { "coreMetadata", "format", "audioFormatExtended" };

        /// <summary>
        /// Root element of the model's contained program.
        /// </summary>
        public const string programTag = "audioProgramme";

        /// <summary>
        /// Name of a program's ID attribute.
        /// </summary>
        public const string programIDAttribute = "audioProgrammeID";

        /// <summary>
        /// Name of a program's name attribute.
        /// </summary>
        public const string programNameAttribute = "audioProgrammeName";

        /// <summary>
        /// Beginning timestamp of a program or object.
        /// </summary>
        public const string startAttribute = "start";

        /// <summary>
        /// End timestamp of a program. Must be start + length.
        /// </summary>
        public const string programEndAttribute = "end";

        /// <summary>
        /// Reference to a group of objects by ID.
        /// </summary>
        public const string contentRefTag = "audioContentIDRef";

        /// <summary>
        /// A group of objects.
        /// </summary>
        public const string contentTag = "audioContent";

        /// <summary>
        /// Name of a content's ID attribute.
        /// </summary>
        public const string contentIDAttribute = "audioContentID";

        /// <summary>
        /// Name of a content's name attribute.
        /// </summary>
        public const string contentNameAttribute = "audioContentName";

        /// <summary>
        /// Dialog mixing method descriptor tag.
        /// </summary>
        public const string contentDialogueTag = "dialogue";

        /// <summary>
        /// Reference to a single audio object by ID.
        /// </summary>
        public const string objectRefTag = "audioObjectIDRef";

        /// <summary>
        /// A single audio object with multiple possible tracks.
        /// </summary>
        public const string objectTag = "audioObject";

        /// <summary>
        /// Name of an object's ID attribute.
        /// </summary>
        public const string objectIDAttribute = "audioObjectID";

        /// <summary>
        /// Name of an object's name attribute.
        /// </summary>
        public const string objectNameAttribute = "audioObjectName";

        /// <summary>
        /// Reference to a pack format by ID.
        /// </summary>
        public const string packFormatRefTag = "audioPackFormatIDRef";

        /// <summary>
        /// Contains position/movement data for all tracks in an object.
        /// </summary>
        public const string packFormatTag = "audioPackFormat";

        /// <summary>
        /// Name of a pack format's ID attribute.
        /// </summary>
        public const string packFormatIDAttribute = "audioPackFormatID";

        /// <summary>
        /// Name of a pack format's name attribute.
        /// </summary>
        public const string packFormatNameAttribute = "audioPackFormatName";

        /// <summary>
        /// Channel/object selector attribute for both pack formats and redundantly by standard in channel formats.
        /// </summary>
        public const string typeAttribute = "typeLabel";

        /// <summary>
        /// Redundant string version of <see cref="typeAttribute"/>.
        /// </summary>
        public const string typeStringAttribute = "typeDefinition";

        /// <summary>
        /// Reference to positional data by ID.
        /// </summary>
        public const string channelFormatRefTag = "audioChannelFormatIDRef";

        /// <summary>
        /// Contains positional data.
        /// </summary>
        public const string channelFormatTag = "audioChannelFormat";

        /// <summary>
        /// Name of a channel format's ID attribute.
        /// </summary>
        public const string channelFormatIDAttribute = "audioChannelFormatID";

        /// <summary>
        /// Name of a channel format's name attribute.
        /// </summary>
        public const string channelFormatNameAttribute = "audioChannelFormatName";

        /// <summary>
        /// One position of an object's movement.
        /// </summary>
        public const string blockTag = "audioBlockFormat";

        /// <summary>
        /// ID of a block format.
        /// </summary>
        public const string blockIDAttribute = "audioBlockFormatID";

        /// <summary>
        /// Offset timestamp of a block.
        /// </summary>
        public const string blockOffsetAttribute = "rtime";

        /// <summary>
        /// Length timestamp of a block or an object.
        /// </summary>
        public const string durationAttribute = "duration";

        /// <summary>
        /// Speaker label for an audio block.
        /// </summary>
        public const string blockLabelAttribute = "speakerLabel";

        /// <summary>
        /// A block's coordinates are stored as cartesian coordinates.
        /// </summary>
        public const string blockCartesianTag = "cartesian";

        /// <summary>
        /// A block's position on one axis.
        /// </summary>
        public const string blockPositionTag = "position";

        /// <summary>
        /// Additional interpolation information.
        /// </summary>
        public const string blockJumpTag = "jumpPosition";

        /// <summary>
        /// Length of the interpolation of a positional timeslot.
        /// </summary>
        public const string blockJumpLengthAttribute = "interpolationLength";

        /// <summary>
        /// Axis of a positional data.
        /// </summary>
        public const string blockCoordinateAttribute = "coordinate";

        /// <summary>
        /// Reference to a track by ID.
        /// </summary>
        public const string trackRefTag = "audioTrackUIDRef";

        /// <summary>
        /// Track coding information.
        /// </summary>
        public const string trackTag = "audioTrackUID";

        /// <summary>
        /// Name of a track's ID attribute.
        /// </summary>
        public const string trackIDAttribute = "UID";

        /// <summary>
        /// <see cref="BitDepth"/> of a track.
        /// </summary>
        public const string trackBitDepthAttribute = "bitDepth";

        /// <summary>
        /// Sample rate of a track.
        /// </summary>
        public const string trackSampleRateAttribute = "sampleRate";

        /// <summary>
        /// Reference to a track format by ID.
        /// </summary>
        public const string trackFormatRefTag = "audioTrackFormatIDRef";

        /// <summary>
        /// Track and format information.
        /// </summary>
        public const string trackFormatTag = "audioTrackFormat";

        /// <summary>
        /// Name of a track format's ID attribute.
        /// </summary>
        public const string trackFormatIDAttribute = "audioTrackFormatID";

        /// <summary>
        /// Name of a track format's name attribute.
        /// </summary>
        public const string trackFormatNameAttribute = "audioTrackFormatName";

        /// <summary>
        /// Attribute containing the encoding format of a track or a stream.
        /// </summary>
        public const string formatDefinitionAttribute = "formatDefinition";

        /// <summary>
        /// The ID of <see cref="formatDefinitionAttribute"/>.
        /// </summary>
        public const string formatLabelAttribute = "formatLabel";

        /// <summary>
        /// Reference to a stream format by ID.
        /// </summary>
        public const string streamFormatRefTag = "audioStreamFormatIDRef";

        /// <summary>
        /// Channel/pack/track format grouping.
        /// </summary>
        public const string streamFormatTag = "audioStreamFormat";

        /// <summary>
        /// Name of a stream format's ID attribute.
        /// </summary>
        public const string streamFormatIDAttribute = "audioStreamFormatID";

        /// <summary>
        /// Name of a stream format's name attribute.
        /// </summary>
        public const string streamFormatNameAttribute = "audioStreamFormatName";
    }
}