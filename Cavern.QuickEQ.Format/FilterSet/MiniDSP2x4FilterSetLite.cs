using System.IO;

using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for MiniDSP 2x4, only using half the bands (used as either input or output filter set).
    /// This class is for the MiniDSP 2x4 Advanced driver.
    /// </summary>
    /// <remarks>MiniDSP hardware only work on 96 kHz sampling rate. Using anything else breaks the filter set.</remarks>
    public class MiniDSP2x4FilterSetLite : MiniDSP2x4FilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 5;

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 with a given number of channels, only using half the bands.
        /// </summary>
        public MiniDSP2x4FilterSetLite(int channels) : base(channels) { }

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 with a given number of channels, only using half the bands.
        /// </summary>
        public MiniDSP2x4FilterSetLite(ReferenceChannel[] channels) : base(channels) { }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            CreateRootFile(path, "txt");
            for (int i = 0; i < Channels.Length; i++) {
                SaveFilters(((IIRChannelData)Channels[i]).filters, 0, Bands, Path.Combine(folder, $"{fileNameBase} {GetLabel(i)}.txt"));
            }
        }
    }

    /// <summary>
    /// IIR filter set for MiniDSP 2x4 HD.
    /// </summary>
    public class MiniDSP2x4HDFilterSetLite : MiniDSP2x4FilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 10;

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 HD with a given number of channels.
        /// </summary>
        public MiniDSP2x4HDFilterSetLite(int channels) : base(channels) { }

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 HD with a given number of channels.
        /// </summary>
        public MiniDSP2x4HDFilterSetLite(ReferenceChannel[] channels) : base(channels) { }
    }
}