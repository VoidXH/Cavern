using System.IO;

using Cavern.Format.Utilities;

namespace Cavern.Format.ConfigurationFile.ConvolutionBoxFormat {
    /// <summary>
    /// Convolution filter in a <see cref="ConvolutionBoxFormatConfigurationFile"/>.
    /// </summary>
    class ConvolutionEntry : CBFEntry {
        /// <summary>
        /// Channel affected by this filter.
        /// </summary>
        public int Channel { get; }

        /// <summary>
        /// Convolution filter samples.
        /// </summary>
        public float[] Filter { get; }

        /// <summary>
        /// Convolution filter in a <see cref="ConvolutionBoxFormatConfigurationFile"/>.
        /// </summary>
        /// <param name="channel">Channel affected by this filter</param>
        /// <param name="filter">Convolution filter samples</param>
        public ConvolutionEntry(int channel, float[] filter) {
            Channel = channel;
            Filter = filter;
        }

        /// <summary>
        /// Convolution filter from a <see cref="ConvolutionBoxFormatConfigurationFile"/> <paramref name="stream"/>.
        /// </summary>
        public ConvolutionEntry(Stream stream) {
            Channel = stream.ReadInt32();
            Filter = new float[stream.ReadInt32()];
            for (int i = 0; i < Filter.Length; i++) {
                Filter[i] = stream.ReadSingle();
            }
        }

        /// <inheritdoc/>
        public override void Write(Stream target) {
            target.WriteByte(1);
            target.WriteAny(Channel);
            target.WriteAny(Filter.Length);
            for (int i = 0; i < Filter.Length; i++) {
                target.WriteAny(Filter[i]);
            }
        }
    }
}