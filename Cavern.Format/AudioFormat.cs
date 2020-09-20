namespace Cavern.Format {
    /// <summary>Supported audio formats in this namespace.</summary>
    public enum AudioFormat {
        /// <summary>Minimal RIFF Wave implementation.</summary>
        RIFFWave,
        /// <summary>Limitless Audio Format, supports spatial mixes.</summary>
        LimitlessAudioFormat,
    }

    /// <summary>Audio bit depth choices.</summary>
    public enum BitDepth { // TODO: support 24-bit integer
        /// <summary>8-bit integer.</summary>
        Int8 = 8,
        /// <summary>16-bit integer.</summary>
        Int16 = 16,
        /// <summary>32-bit floating point.</summary>
        Float32 = 32,
    }
}