namespace Cavern.Format.Consts {
    /// <summary>
    /// Constants used for the MP4 file format.
    /// </summary>
    internal static class MP4Consts {
        /// <summary>
        /// FourCC marker of the file type box (ftyp).
        /// </summary>
        public const uint fileTypeBox = 0x66747970;

        /// <summary>
        /// FourCC marker of the boxes to skip (free).
        /// </summary>
        public const uint freeBox = 0x66726565;

        /// <summary>
        /// FourCC marker of the metadata box (moov).
        /// </summary>
        public const uint metadataBox = 0x6D6F6F76;

        /// <summary>
        /// FourCC marker of the <see cref="metadataBox"/>'s header box (mvhd).
        /// </summary>
        public const uint metadataHeaderBox = 0x6D766864;

        /// <summary>
        /// FourCC marker of the box where raw bytes of the tracks can be found (mdat).
        /// </summary>
        public const uint rawBox = 0x6D646174;

        /// <summary>
        /// FourCC marker of the track metadata box (trak).
        /// </summary>
        public const uint trackBox = 0x7472616B;

        /// <summary>
        /// FourCC marker of the <see cref="trackBox"/>'s header child box (tkhd).
        /// </summary>
        public const uint trackHeaderBox = 0x746B6864;

        /// <summary>
        /// FourCC marker of the <see cref="trackBox"/>'s media child box (mdia).
        /// </summary>
        public const uint mediaBox = 0x6D646961;

        /// <summary>
        /// FourCC marker of the <see cref="mediaBox"/>'s media header box (mdhd).
        /// </summary>
        public const uint mediaHeaderBox = 0x6D646864;

        /// <summary>
        /// FourCC marker of the <see cref="mediaBox"/>'s media information box (minf).
        /// </summary>
        public const uint mediaInfoBox = 0x6D696E66;

        /// <summary>
        /// FourCC marker of the <see cref="mediaInfoBox"/>'s sample table box (stbl).
        /// </summary>
        public const uint sampleTableBox = 0x7374626C;

        /// <summary>
        /// FourCC marker of the <see cref="sampleTableBox"/>'s sample description box (stsd).
        /// </summary>
        public const uint sampleDescriptionBox = 0x73747364;
    }
}