using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for Yamaha RX-A series AVRs.
    /// </summary>
    public class YamahaRXAFilterSet : YPAOFilterSet {
        /// <inheritdoc/>
        protected override float[] Frequencies => frequencies;

        /// <summary>
        /// IIR filter set for Yamaha RX-A series AVRs.
        /// </summary>
        public YamahaRXAFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for Yamaha RX-A series AVRs.
        /// </summary>
        public YamahaRXAFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// All the possible bands that can be selected for Yamaha RX-A series amplifiers. These are 1/3 octaves apart.
        /// </summary>
        static readonly float[] frequencies = {
            15.6f, 16.6f, 17.5f, 18.6f, 19.7f, 20.9f, 22.1f, 23.4f, 24.8f, 26.3f, 27.8f, 29.5f, 31.3f, 33.1f, 35.1f, 37.2f, 39.4f, 42,
            44.2f, 47, 49.6f, 53, 55.7f, 59, 62.5f, 66, 70.2f, 74, 78.7f, 83.4f, 88.4f, 93.6f, 99.2f, 105.1f, 111.4f, 118, 125, 132.4f,
            140.3f, 148.7f, 157.5f, 166.9f, 176.8f, 187.3f, 198.4f, 210.2f, 222.7f, 236, 250, 264.9f, 280.6f, 297.3f, 315, 333.7f, 353.6f,
            374.6f, 396.9f, 420.4f, 445.4f, 471.9f, 500, 529.7f, 561.2f, 594.6f, 630, 667.4f, 707.1f, 749.2f, 793.7f, 840.9f, 890.9f,
            943.9f, 1000, 1060, 1120, 1190, 1260, 1330, 1410, 1500, 1590, 1680, 1780, 1890, 2000, 2120, 2240, 2380, 2520, 2670, 2830, 3000,
            3170, 3360, 3560, 3780, 4000, 4240, 4490, 4760, 5040, 5340, 5660, 5990, 6350, 6730, 7130, 7550, 8000, 8480, 8980, 9510, 10100,
            10700, 11300, 12000, 12700, 13500, 14300, 15100, 16000
        };
    }
}