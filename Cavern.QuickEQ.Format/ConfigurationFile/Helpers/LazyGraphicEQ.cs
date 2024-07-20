using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using Cavern.Filters;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.Format.ConfigurationFile.Helpers {
    /// <summary>
    /// Placeholder where a <see cref="GraphicEQ"/> should be created.
    /// </summary>
    public sealed class LazyGraphicEQ : Filter, ILazyLoadableFilter, IXmlSerializable {
        /// <summary>
        /// Desired frequency response change.
        /// </summary>
        Equalizer equalizer;

        /// <summary>
        /// Sample rate at which this EQ is converted to a minimum-phase FIR filter.
        /// </summary>
        int sampleRate;

        /// <summary>
        /// Placeholder where a <see cref="GraphicEQ"/> should be created.
        /// </summary>
        public LazyGraphicEQ(Equalizer equalizer, int sampleRate) {
            this.equalizer = equalizer;
            this.sampleRate = sampleRate;
        }

        /// <inheritdoc/>
        public override void Process(float[] samples) => throw new PlaceholderFilterException();

        /// <inheritdoc/>
        public Filter CreateFilter(FFTCachePool cachePool) {
            FFTCache cache = cachePool.Lease();
            Filter result = new GraphicEQ(equalizer, sampleRate, cache);
            cachePool.Return(cache);
            return result;
        }

        /// <inheritdoc/>
        public override object Clone() => new LazyGraphicEQ(equalizer, sampleRate);

        /// <inheritdoc/>
        public XmlSchema GetSchema() => null;

        /// <inheritdoc/>
        public void ReadXml(XmlReader reader) {
            while (reader.MoveToNextAttribute()) {
                switch (reader.Name) {
                    case nameof(GraphicEQ.SampleRate):
                        sampleRate = int.Parse(reader.Value);
                        break;
                    case nameof(GraphicEQ.Equalizer):
                        equalizer = EQGenerator.FromEqualizerAPO(reader.Value);
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(nameof(GraphicEQ));
            writer.WriteAttributeString(nameof(GraphicEQ.SampleRate), sampleRate.ToString());
            writer.WriteAttributeString(nameof(GraphicEQ.Equalizer), equalizer.ExportToEqualizerAPO());
            writer.WriteEndElement();
        }
    }
}