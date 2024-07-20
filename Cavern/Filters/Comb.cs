using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using Cavern.Filters.Interfaces;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Normalized feedforward comb filter.
    /// </summary>
    /// <remarks>The feedback comb filter is called <see cref="Echo"/>.</remarks>
    public class Comb : Filter, ILocalizableToString, ISampleRateDependentFilter, IXmlSerializable {
        /// <inheritdoc/>
        [IgnoreDataMember]
        public int SampleRate {
            get => sampleRate;
            set {
                double oldFrequency = Frequency;
                sampleRate = value;
                Frequency = oldFrequency;
            }
        }
        int sampleRate;

        /// <summary>
        /// Wet mix multiplier.
        /// </summary>
        [DisplayName("Alpha (ratio)")]
        public double Alpha { get; set; }

        /// <summary>
        /// Delay in samples.
        /// </summary>
        [DisplayName("K (samples)")]
        public int K {
            get => delay.DelaySamples;
            set => delay.DelaySamples = value;
        }

        /// <summary>
        /// Delay in milliseconds.
        /// </summary>
        [DisplayName("K (ms)")]
        public double DelayMs {
            get => delay.DelayMs;
            set => delay.DelayMs = value;
        }

        /// <summary>
        /// First minimum point.
        /// </summary>
        [DisplayName("Frequency (Hz)")]
        public double Frequency {
            get => sampleRate * .5 / K;
            set => K = (int)(.5 / (value / sampleRate) + 1);
        }

        /// <summary>
        /// Delay filter generating the samples fed forward.
        /// </summary>
        readonly Delay delay;

        /// <summary>
        /// Array used to hold samples processed by <see cref="delay"/>.
        /// </summary>
        float[] cache = new float[0];

        /// <summary>
        /// Normalized feedforward comb filter.
        /// </summary>
        /// <param name="sampleRate">Source sample rate</param>
        /// <param name="K">Delay in samples</param>
        /// <param name="alpha">Wet mix multiplier</param>
        public Comb(int sampleRate, int K, double alpha) {
            this.sampleRate = sampleRate;
            Alpha = alpha;
            delay = new Delay(K) {
                SampleRate = sampleRate
            };
        }

        /// <summary>
        /// Normalized feedforward comb filter.
        /// </summary>
        /// <param name="sampleRate">Source sample rate</param>
        /// <param name="frequency">First minimum point</param>
        /// <param name="alpha">Wet mix multiplier</param>
        public Comb(int sampleRate, double frequency, double alpha) {
            this.sampleRate = sampleRate;
            Alpha = alpha;
            delay = new Delay((int)(.5 / (frequency / sampleRate) + 1)) {
                SampleRate = sampleRate
            };
        }

        /// <inheritdoc/>
        public override void Process(float[] samples) {
            if (cache.Length != samples.Length) {
                cache = new float[samples.Length];
            }
            Array.Copy(samples, cache, samples.Length);
            delay.Process(cache);
            float alpha = (float)Alpha,
                divisor = 1 / (1 + alpha);
            for (int sample = 0; sample < samples.Length; sample++) {
                samples[sample] = (samples[sample] + cache[sample] * alpha) * divisor;
            }
        }

        /// <inheritdoc/>
        public override object Clone() => new Comb(sampleRate, K, Alpha);

        /// <inheritdoc/>
        public XmlSchema GetSchema() => null;

        /// <inheritdoc/>
        public void ReadXml(XmlReader reader) {
            while (reader.MoveToNextAttribute()) {
                switch (reader.Name) {
                    case nameof(SampleRate):
                        sampleRate = int.Parse(reader.Value);
                        break;
                    case nameof(K):
                        K = int.Parse(reader.Value);
                        break;
                    case nameof(Alpha):
                        Alpha = QMath.ParseDouble(reader.Value);
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(nameof(Comb));
            writer.WriteAttributeString(nameof(SampleRate), sampleRate.ToString());
            writer.WriteAttributeString(nameof(K), K.ToString());
            writer.WriteAttributeString(nameof(Alpha), Alpha.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public override string ToString() =>
            $"Comb: {QMath.ToStringLimitDecimals(Alpha, 3)}x, {QMath.ToStringLimitDecimals(DelayMs, 3)} ms";

        /// <inheritdoc/>
        public string ToString(CultureInfo culture) => culture.Name switch {
            "hu-HU" => $"Fésű: {QMath.ToStringLimitDecimals(Alpha, 3)}x, {QMath.ToStringLimitDecimals(DelayMs, 3)} ms",
            _ => ToString()
        };
    }
}