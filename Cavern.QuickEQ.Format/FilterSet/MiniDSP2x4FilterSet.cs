using System.Globalization;
using System.IO;

using Cavern.Channels;
using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for MiniDSP 2x4. This class is for the MiniDSP 2x4 Advanced driver.
    /// </summary>
    /// <remarks>MiniDSP 2x4s only work on 96 kHz sampling rate. Using anything else breaks the filter set.</remarks>
    public class MiniDSP2x4FilterSet : IIRFilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 10;

        /// <summary>
        /// Minimum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MinGain => -100;

        /// <summary>
        /// Maximum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MaxGain => 20;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public override double GainPrecision => .01;

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 with a given number of channels.
        /// </summary>
        public MiniDSP2x4FilterSet(int channels) : this(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 with a given set of channels.
        /// </summary>
        public MiniDSP2x4FilterSet(ReferenceChannel[] channels) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 with a given number of channels and at a modified sample rate.
        /// </summary>
        internal MiniDSP2x4FilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 with a given set of channels and at a modified sample rate.
        /// </summary>
        internal MiniDSP2x4FilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            CreateRootFile(path, "txt");

            for (int i = 0; i < Channels.Length; i++) {
                string pathBase = Path.Combine(folder, $"{fileNameBase} {GetLabel(i)} ");
                BiquadFilter[] filters = ((IIRChannelData)Channels[i]).filters;
                SaveFilters(filters, 0, Bands >> 1, pathBase + "input.txt");
                SaveFilters(filters, Bands >> 1, Bands, pathBase + "output.txt");
            }
        }

        /// <summary>
        /// Save a partial filter set in MiniDSP's format.
        /// </summary>
        protected void SaveFilters(BiquadFilter[] filters, int from, int to, string path) {
            string[] lines = new string[(to - from) * 6];
            if (to > filters.Length) {
                to = filters.Length;
            }
            --to;
            int line = 0;
            for (int i = from; i <= to; i++) {
                lines[line++] = $"biquad{i - from + 1},";
                lines[line++] = $"b0={filters[i].b0.ToString(CultureInfo.InvariantCulture)},";
                lines[line++] = $"b1={filters[i].b1.ToString(CultureInfo.InvariantCulture)},";
                lines[line++] = $"b2={filters[i].b2.ToString(CultureInfo.InvariantCulture)},";
                lines[line++] = $"a1={(-filters[i].a1).ToString(CultureInfo.InvariantCulture)},";
                lines[line++] = string.Format(i != to ? "a2={0}," : "a2={0}", (-filters[i].a2).ToString(CultureInfo.InvariantCulture));
            }
            File.WriteAllLines(path, lines);
        }

        /// <summary>
        /// Working sample rate of all MiniDSP 2x4s.
        /// </summary>
        const int sampleRate = 96000;
    }

    /// <summary>
    /// IIR filter set for MiniDSP 2x4 HD.
    /// </summary>
    public class MiniDSP2x4HDFilterSet : MiniDSP2x4FilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 20;

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 HD with a given number of channels.
        /// </summary>
        public MiniDSP2x4HDFilterSet(int channels) : base(channels) { }

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 HD with a given set of channels.
        /// </summary>
        public MiniDSP2x4HDFilterSet(ReferenceChannel[] channels) : base(channels) { }
    }
}