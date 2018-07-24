using System.Collections.Generic;

namespace Cavern.QuickEQ {
    /// <summary>Equalizer data collector and exporter.</summary>
    public class Equalizer {
        /// <summary>A single equalizer band.</summary>
        public struct Band {
            /// <summary>Position of the band.</summary>
            public float Frequency;
            /// <summary>Gain at <see cref="Frequency"/> in dB.</summary>
            public float Gain;

            /// <summary>EQ band constructor.</summary>
            public Band(float Frequency, float Gain) {
                this.Frequency = Frequency;
                this.Gain = Gain;
            }
        }

        /// <summary>Bands that make up this equalizer.</summary>
        List<Band> Bands = new List<Band>();

        /// <summary>Add a new band to the EQ.</summary>
        public void AddBand(Band NewBand) {
            Bands.Add(NewBand);
        }

        // TODO: export as configuration file for some EQ applications

        /// <summary>Generate an equalizer setting to flatten the received response.</summary>
        public static Equalizer CorrectResponse(float[] Reference, float[] Response, float FreqStart, float FreqEnd, int SampleRate) {
            float[] Curve = Measurements.GetFrequencyResponse(Reference, Response, FreqStart, FreqEnd, SampleRate);
            Curve = Measurements.SmoothResponse(Curve, FreqStart, FreqEnd);
            Equalizer Result = new Equalizer();
            // TODO: add bands
            return Result;
        }
    }
}