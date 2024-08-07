﻿using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using Cavern.Filters.Interfaces;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Simple convolution window filter.
    /// </summary>
    /// <remarks>This filter is performing convolution by definition, which is faster if the window size is very small.
    /// For most cases, <see cref="FastConvolver"/> is preferred.</remarks>
    public class Convolver : Filter, IConvolution, ILocalizableToString, IXmlSerializable {
        /// <inheritdoc/>
        [IgnoreDataMember]
        public int SampleRate { get; set; }

        /// <inheritdoc/>
        public float[] Impulse {
            get => impulse;
            set {
                if (future.Length != (impulse = value).Length) {
                    future = new float[value.Length + delay];
                }
            }
        }

        /// <summary>
        /// Additional impulse delay in samples.
        /// </summary>
        public int Delay {
            get => delay;
            set => future = new float[impulse.Length + (delay = value)];
        }

        /// <summary>
        /// Additional impulse delay in samples.
        /// </summary>
        protected int delay;

        /// <summary>
        /// Impulse response to convolve with.
        /// </summary>
        protected float[] impulse;

        /// <summary>
        /// Samples to be copied to the beginning of the next output.
        /// </summary>
        protected float[] future;

        /// <summary>
        /// Construct a convolver for a target impulse response.
        /// </summary>
        /// <param name="impulse">Impulse response to convolve with</param>
        /// <param name="delay">Additional impulse delay in samples</param>
        public Convolver(float[] impulse, int delay) {
            this.impulse = impulse;
            Delay = delay;
        }

        /// <summary>
        /// Construct a convolver for a target impulse response.
        /// </summary>
        /// <param name="impulse">Impulse response to convolve with</param>
        /// <param name="sampleRate">Sample rate of the <paramref name="impulse"/> response</param>
        /// <param name="delay">Additional impulse delay in samples</param>
        public Convolver(float[] impulse, int sampleRate, int delay) : this(impulse, delay) => SampleRate = sampleRate;

        /// <summary>
        /// Perform a convolution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] Convolve(float[] a, float[] b) {
            float[] convolved = new float[a.Length + b.Length];
            for (int i = 0; i < a.Length; ++i) {
                for (int j = 0; j < b.Length; ++j) {
                    convolved[i + j] += a[i] * b[j];
                }
            }
            return convolved;
        }

        /// <summary>
        /// Perform a convolution with a delay.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] Convolve(float[] a, float[] b, int delay) {
            float[] convolved = new float[a.Length + b.Length + delay];
            for (int i = 0; i < a.Length; ++i) {
                for (int j = 0; j < b.Length; ++j) {
                    convolved[i + j + delay] += a[i] * b[j];
                }
            }
            return convolved;
        }

        /// <inheritdoc/>
        public override void Process(float[] samples) {
            float[] convolved;
            if (delay == 0) {
                convolved = Convolve(samples, impulse);
            } else {
                convolved = Convolve(samples, impulse, delay);
            }
            Finalize(samples, convolved);
        }

        /// <inheritdoc/>
        public override object Clone() => new Convolver((float[])impulse.Clone(), SampleRate, delay);

        /// <inheritdoc/>
        public XmlSchema GetSchema() => null;

        /// <inheritdoc/>
        public virtual void ReadXml(XmlReader reader) => this.ReadCommonXml(reader);

        /// <inheritdoc/>
        public virtual void WriteXml(XmlWriter writer) => this.WriteCommonXml(writer, nameof(Convolver));

        /// <inheritdoc/>
        public override string ToString() => "Convolution";

        /// <inheritdoc/>
        public string ToString(CultureInfo culture) => culture.Name switch {
            "hu-HU" => "Konvolúció",
            _ => ToString()
        };

        /// <summary>
        /// Output the result and handle the future.
        /// </summary>
        protected void Finalize(float[] samples, float[] convolved) {
            int delayedImpulse = impulse.Length + delay;
            if (samples.Length > delayedImpulse) {
                // Drain cache
                Array.Copy(convolved, 0, samples, 0, samples.Length);
                for (int sample = 0; sample < delayedImpulse; ++sample) {
                    samples[sample] += future[sample];
                }
                // Fill cache
                Array.Copy(convolved, samples.Length, future, 0, delayedImpulse);
            } else {
                // Drain cache
                for (int sample = 0; sample < samples.Length; ++sample) {
                    samples[sample] = convolved[sample] + future[sample];
                }
                // Move cache
                int futureEnd = delayedImpulse - samples.Length;
                Array.Copy(future, samples.Length, future, 0, futureEnd);
                Array.Clear(future, futureEnd, samples.Length);
                // Merge cache
                for (int sample = 0; sample < delayedImpulse; ++sample) {
                    future[sample] += convolved[sample + samples.Length];
                }
            }
        }
    }
}