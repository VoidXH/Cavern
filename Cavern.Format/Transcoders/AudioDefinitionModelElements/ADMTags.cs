namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains tags that describe an ADM metadata. The structure is the following:<br/>
    /// - <see cref="ADMProgramme"/>s<br/>
    /// -- <see cref="ADMContent"/>s<br/>
    /// --- <see cref="ADMObject"/>s<br/>
    /// ---- <see cref="ADMPackFormat"/><br/>
    /// ----- <see cref="ADMChannelFormat"/><br/>
    /// ---- Tracks (redundant - found in WAV header)<br/>
    /// ----- Track format (redundant - found in WAV header)<br/>
    /// ------ StreamFormat (redundant by standard)
    /// </summary>
    static class ADMTags {
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
        /// Reference to a single audio object by ID.
        /// </summary>
        public static string objectRefTag = "audioObjectIDRef";

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
        public const string packFormatTypeAttribute = "typeLabel";

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
        /// Length timestamp of a block.
        /// </summary>
        public const string blockDurationAttribute = "duration";

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
    }
}