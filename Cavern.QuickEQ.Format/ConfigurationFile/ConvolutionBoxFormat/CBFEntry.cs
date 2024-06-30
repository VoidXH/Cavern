using System;
using System.IO;

namespace Cavern.Format.ConfigurationFile.ConvolutionBoxFormat {
    /// <summary>
    /// A single filter to be written into a <see cref="ConvolutionBoxFormatConfigurationFile"/>.
    /// </summary>
    abstract class CBFEntry {
        /// <summary>
        /// Export this filter.
        /// </summary>
        public abstract void Write(Stream target);

        /// <summary>
        /// Get the next entry from a <see cref="ConvolutionBoxFormatConfigurationFile"/> <paramref name="stream"/>.
        /// </summary>
        public static CBFEntry Read(Stream stream) => stream.ReadByte() switch {
            0 => new MatrixEntry(stream),
            1 => new ConvolutionEntry(stream),
            _ => throw new NotImplementedException(),
        };
    }
}