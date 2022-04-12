namespace Cavern.Format.Transcoders {
    static partial class EnhancedAC3 {
        /// <summary>
        /// Supported decoder versions.
        /// </summary>
        public enum Decoders {
            AlternateAC3 = 6,
            AC3 = 8,
            EAC3 = 16
        }

        /// <summary>
        /// Types of programs in a single frame of a stream.
        /// </summary>
        public enum StreamTypes {
            /// <summary>
            /// Main program, can be decoded on its own.
            /// </summary>
            Independent,
            /// <summary>
            /// Should be decoded with the associated independent substream.
            /// </summary>
            Dependent,
            /// <summary>
            /// This frame was converted from AC-3, the E-AC-3 extra data will follow.
            /// Usually used to go beyond 5.1, up to 16 discrete channels.
            /// </summary>
            Repackaged,
            /// <summary>
            /// Unused type.
            /// </summary>
            Reserved
        }
    }
}