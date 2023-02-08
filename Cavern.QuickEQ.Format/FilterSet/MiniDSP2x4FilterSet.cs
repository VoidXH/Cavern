using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for MiniDSP 2x4. This class is for the MiniDSP 2x4 Advanced driver.
    /// </summary>
    /// <remarks>MiniDSP hardware only work on 96 kHz sampling rate. Using anything else breaks the filter set.</remarks>
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
        public MiniDSP2x4FilterSet(int channels) : base(channels, 96000) { }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            CreateRootFile(path, "txt");

            int split = (Bands >> 1) - 1;
            for (int i = 0, c = Channels.Length; i < c; ++i) {
                List<string> inputData = new List<string>(), outputData = new List<string>();
                BiquadFilter[] filters = Channels[i].filters;
                for (int j = 0; j < filters.Length; j++) {
                    List<string> targetData = j < (Bands >> 1) ? inputData : outputData;
                    targetData.AddRange(new string[] {
                        $"biquad{j % (Bands >> 1) + 1},",
                        $"b0={filters[j].b0.ToString(CultureInfo.InvariantCulture)},",
                        $"b1={filters[j].b1.ToString(CultureInfo.InvariantCulture)},",
                        $"b2={filters[j].b2.ToString(CultureInfo.InvariantCulture)},",
                        $"a1={(-filters[j].a1).ToString(CultureInfo.InvariantCulture)},",
                        string.Format(j % (Bands >> 1) != split ? "a2={0}," : "a2={0}",
                            (-filters[j].a2).ToString(CultureInfo.InvariantCulture))
                    });
                }

                string pathBase = Path.Combine(folder, $"{fileNameBase} {GetLabel(i)} ");
                File.WriteAllLines(pathBase + "input.txt", inputData);
                File.WriteAllLines(pathBase + "output.txt", outputData);
            }
        }
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
    }
}