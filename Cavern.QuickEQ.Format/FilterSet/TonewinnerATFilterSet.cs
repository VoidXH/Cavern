using System.IO;

using Cavern.Channels;
using Cavern.Format.FilterSet.BaseClasses;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for Tonewinner AT-series processors.
    /// </summary>
    public class TonewinnerATFilterSet : LimitedIIRFilterSet {
        /// <inheritdoc/>
        public override int Bands => throw new ChannelDependentBandCountException();

        /// <inheritdoc/>
        public override int LFEBands => throw new ChannelDependentBandCountException();

        /// <inheritdoc/>
        public override double MinGain => -12;

        /// <inheritdoc/>
        public override double MaxGain => 12;

        /// <inheritdoc/>
        public override double GainPrecision => .5;

        /// <inheritdoc/>
        protected override float[] Frequencies => frequencies;

        /// <inheritdoc/>
        protected override float[] QFactors => qFactors;

        /// <summary>
        /// IIR filter set for Tonewinner AT-series processors.
        /// </summary>
        public TonewinnerATFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for Tonewinner AT-series processors.
        /// </summary>
        public TonewinnerATFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        public override int GetBands(ReferenceChannel channel) {
            switch (channel) {
                case ReferenceChannel.FrontLeft:
                case ReferenceChannel.FrontRight:
                case ReferenceChannel.FrontCenter:
                case ReferenceChannel.FrontLeftCenter:
                case ReferenceChannel.FrontRightCenter:
                    return 11;
                case ReferenceChannel.ScreenLFE:
                    return 5;
                case ReferenceChannel.TopFrontLeft:
                case ReferenceChannel.TopFrontRight:
                case ReferenceChannel.TopSideLeft:
                case ReferenceChannel.TopSideRight:
                case ReferenceChannel.TopFrontCenter:
                case ReferenceChannel.TopRearLeft:
                case ReferenceChannel.TopRearRight:
                case ReferenceChannel.TopRearCenter:
                    return 7;
                default:
                    return 6;
            }
        }

        /// <inheritdoc/>
        public override void Export(string path) => File.WriteAllText(path, Export(false));

        /// <summary>
        /// All the possible bands that can be selected for this device.
        /// </summary>
        static readonly float[] frequencies = {
            20.2f, 20.8f, 21.3f, 21.9f, 22.5f, 23.2f, 23.8f, 24.4f, 25.1f, 25.8f, 26.5f, 27.2f, 28, 28.7f, 29.5f, 30.3f, 31.2f, 32, 32.9f,
            33.8f, 34.7f, 35.7f, 36.7f, 37.7f, 38.7f, 39.8f, 40.8f, 42, 43.1f, 44.3f, 45.5f, 46.7f, 48, 49.3f, 50.7f, 52.1f, 53.5f, 55,
            56.5f, 58, 59.6f, 61.2f, 62.9f, 64.6f, 66.4f, 68.2f, 70.1f, 72, 74, 76f, 78.1f, 80.2f, 82.4f, 84.7f, 87, 89.4f, 91.8f, 94.3f,
            96.9f, 99.6f, 102, 105, 108, 111, 114, 117, 120, 124, 127, 130, 134, 138, 141, 145, 149, 153, 158, 162, 166, 171, 176, 180,
            185, 190, 196, 201, 206, 212, 218, 224, 230, 236, 243, 249, 256, 263, 270, 278, 285, 293, 301, 309, 318, 327, 335, 345, 354,
            364, 374, 384, 394, 405, 416, 428, 439, 451, 464, 476, 489, 503, 517, 531, 545, 560, 576, 591, 607, 624, 641, 659, 677, 695,
            714, 734, 754, 774, 796, 817, 840, 863, 886, 910, 935, 961, 987, 1010, 1040, 1070, 1090, 1120, 1160, 1190, 1220, 1250, 1290,
            1320, 1360, 1400, 1440, 1470, 1520, 1560, 1600, 1640, 1690, 1730, 1780, 1830, 1880, 1930, 1990, 2040, 2100, 2150, 2210, 2270,
            2340, 2400, 2470, 2530, 2600, 2670, 2750, 2820, 2900, 2980, 3060, 3150, 3230, 3320, 3410, 3500, 3600, 3700, 3800, 3900, 4010,
            4120, 4230, 4350, 4470, 4590, 4720, 4850, 4980, 5120, 5260, 5400, 5550, 5700, 5850, 6010, 6180, 6350, 6520, 6700, 6880, 7070,
            7270, 7470, 7670, 7880, 8100, 8320, 8540, 8780, 9020, 9270, 9520, 9780, 10050, 10320, 10600, 10890, 11190, 11500, 11810, 12140,
            12470, 12810, 13160, 13520, 13890, 14270, 14660, 15060, 15470, 15900, 16330, 16780, 17240, 17710, 18190, 18690, 19200, 19730
        };

        /// <summary>
        /// All the possible Q-factors that can be selected for this device.
        /// </summary>
        static readonly float[] qFactors = { 0.12f, 25, 37, 50, 62, 75, 87, 1, 1.12f, 1.25f, 1.37f, 1.50f, 1.62f, 1.75f, 1.87f, 2, 2.25f,
            2.50f, 2.75f, 3, 3.25f, 3.5f, 3.75f, 4, 4.25f, 4.5f, 4.75f, 5, 5.5f, 6, 6.5f, 7, 8, 9, 10, 12, 14, 16, 18, 20, 22, 24 };
    }
}