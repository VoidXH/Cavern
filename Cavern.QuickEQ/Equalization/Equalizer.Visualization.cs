using System;
using System.Globalization;
using System.IO;
using System.Text;

using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Equalization {
    partial class Equalizer {
        /// <summary>
        /// Shows the resulting frequency response if this EQ is applied.
        /// </summary>
        /// <param name="response">Frequency response curve to apply the EQ on, from
        /// <see cref="GraphUtils.ConvertToGraph(float[], double, double, int, int)"/></param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public float[] Apply(float[] response, double startFreq, double endFreq) {
            float[] filter = Visualize(startFreq, endFreq, response.Length);
            for (int i = 0; i < response.Length; i++) {
                filter[i] += response[i];
            }
            return filter;
        }

        /// <summary>
        /// Apply this EQ on a frequency response.
        /// </summary>
        /// <param name="response">Frequency response to apply the EQ on</param>
        /// <param name="sampleRate">Sample rate where <paramref name="response"/> was generated</param>
        public void Apply(Complex[] response, int sampleRate) {
            int halfLength = response.Length / 2 + 1, nyquist = sampleRate / 2;
            float[] filter = VisualizeLinear(0, nyquist, halfLength);
            response[0] *= (float)Math.Pow(10, filter[0] * .05f);
            for (int i = 1; i < halfLength; i++) {
                response[i] *= (float)Math.Pow(10, filter[i] * .05f);
                response[^i] = new Complex(response[i].Real, -response[i].Imaginary);
            }
        }

        /// <summary>
        /// Deserialize this EQ from a binary stream written with <see cref="BinarySerialize(BinaryWriter, bool)"/>.
        /// </summary>
        /// <param name="reader"></param>
        public void BinaryDeserialize(BinaryReader reader) {
            int bandc = reader.ReadInt32();
            bands.Clear();
            if (bandc > 0) { // Lossy
                for (int i = 0; i < bandc; i++) {
                    bands.Add(new Band(reader.ReadSingle(), reader.ReadSingle()));
                }
            } else {
                bandc = -bandc;
                for (int i = 0; i < bandc; i++) {
                    bands.Add(new Band(reader.ReadDouble(), reader.ReadDouble()));
                }
            }
        }

        /// <summary>
        /// Serialize this EQ to any binary stream to later be deserialized with <see cref="BinaryDeserialize(BinaryReader)"/>.
        /// </summary>
        /// <param name="writer">Output writer instance</param>
        /// <param name="lossy">Use single precision instead of double for minimal data loss and half stream sizes</param>
        public void BinarySerialize(BinaryWriter writer, bool lossy = false) {
            if (lossy) {
                writer.Write(bands.Count);
                for (int i = 0, c = bands.Count; i < c; i++) {
                    writer.Write((float)bands[i].Frequency);
                    writer.Write((float)bands[i].Gain);
                }
            } else {
                writer.Write(-bands.Count);
                for (int i = 0, c = bands.Count; i < c; i++) {
                    writer.Write(bands[i].Frequency);
                    writer.Write(bands[i].Gain);
                }
            }
        }

        /// <summary>
        /// Save this EQ to a file in the standard curve/calibration format.
        /// </summary>
        /// <param name="path">Export path of the file</param>
        /// <param name="offset">Gain offset in decibels</param>
        /// <param name="header">Extra text to be added to the first line of the file</param>
        public void Export(string path, double offset, string header = null) {
            int start = header != null ? 1 : 0, c = bands.Count;
            string[] calFile = new string[bands.Count + start];
            if (header != null) {
                calFile[0] = header;
            }

            CultureInfo culture = CultureInfo.InvariantCulture;
            for (int band = 0; band < c; band++) {
                double level = bands[band].Gain + offset;
                calFile[band + start] = $"{bands[band].Frequency.ToString(culture)} {level.ToString(culture)}";
            }
            File.WriteAllLines(path, calFile);
        }

        /// <summary>
        /// Save this EQ to a file in Dirac's curve format.
        /// </summary>
        /// <param name="path">Export path of the file</param>
        /// <param name="offset">Gain offset in decibels</param>
        /// <param name="header">Extra text to be added to the first line of the file</param>
        public void ExportToDirac(string path, double offset, string header = null) {
            int start = header != null ? 2 : 1, c = bands.Count;
            string[] calFile = new string[1 + diracFooter.Length + bands.Count + start];
            if (header != null) {
                calFile[0] = '#' + header;
            }
            calFile[start - 1] = "BREAKPOINTS";

            CultureInfo culture = CultureInfo.InvariantCulture;
            for (int band = 0; band < c; band++) {
                double level = bands[band].Gain + offset;
                calFile[band + start] = $"{bands[band].Frequency.ToString(culture)} {level.ToString(culture)}";
            }

            Array.Copy(diracFooter, 0, calFile, start + c, diracFooter.Length);
            File.WriteAllLines(path, calFile);
        }

        /// <summary>
        /// Get a line in an Equalizer APO configuration file that applies this EQ.
        /// </summary>
        public string ExportToEqualizerAPO() {
            StringBuilder result = new StringBuilder("GraphicEQ:");
            int band = 0, c = bands.Count;
            while (band < c) {
                result.Append(' ').Append(bands[band].Frequency.ToString(CultureInfo.InvariantCulture))
                    .Append(' ').Append(bands[band].Gain.ToString(CultureInfo.InvariantCulture));
                if (++band != c) {
                    result.Append(';');
                } else {
                    break;
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Shows the EQ curve in a logarithmically scaled frequency axis.
        /// </summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] Visualize(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count;
            if (bandCount == 0) {
                return result;
            }
            double mul = Math.Pow(10, (Math.Log10(endFreq) - Math.Log10(startFreq)) / (length - 1));
            for (int i = 0, nextBand = 0, prevBand = 0; i < length; i++) {
                while (nextBand != bandCount && bands[nextBand].Frequency < startFreq) {
                    prevBand = nextBand;
                    nextBand++;
                }
                if (nextBand != bandCount && nextBand != 0) {
                    result[i] = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, startFreq));
                } else {
                    result[i] = (float)bands[prevBand].Gain;
                }
                startFreq *= mul;
            }
            return result;
        }

        /// <summary>
        /// Shows the EQ curve in a linearly scaled frequency axis.
        /// </summary>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="length">Points on the curve</param>
        public float[] VisualizeLinear(double startFreq, double endFreq, int length) {
            float[] result = new float[length];
            int bandCount = bands.Count;
            if (bandCount == 0) {
                return result;
            }
            double step = (endFreq - startFreq) / (length - 1);
            for (int entry = 0, nextBand = 0, prevBand = 0; entry < length; entry++) {
                double freq = startFreq + step * entry;
                while (nextBand != bandCount && bands[nextBand].Frequency < freq) {
                    prevBand = nextBand;
                    nextBand++;
                }
                if (nextBand != bandCount && nextBand != 0) {
                    result[entry] = (float)QMath.Lerp(bands[prevBand].Gain, bands[nextBand].Gain,
                        QMath.LerpInverse(bands[prevBand].Frequency, bands[nextBand].Frequency, freq));
                } else {
                    result[entry] = (float)bands[prevBand].Gain;
                }
            }
            return result;
        }

        /// <summary>
        /// End of a Dirac calibration file. We don't need these features for FIR.
        /// </summary>
        static readonly string[] diracFooter = { "HPSLOPEON", "0", "LPSLOPEON", "0", "HPCUTOFF", "10", "LPCUTOFF", "24000",
            "HPORDER", "4", "LPORDER", "4", "LOWLIMITHZ", "10", "HIGHLIMITHZ", "24000" };
    }
}