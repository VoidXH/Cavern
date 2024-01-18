using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using Cavern.Channels;
using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for Monoprice Monolith HTP-1.
    /// </summary>
    public class MonolithHTP1FilterSet : IIRFilterSet {
        /// <inheritdoc/>
        public override int Bands => 16;

        /// <inheritdoc/>
        public override double MinGain => -18;

        /// <inheritdoc/>
        public override double MaxGain => 18;

        /// <inheritdoc/>
        public override double GainPrecision => .1;

        /// <summary>
        /// IIR filter set for Monoprice Monolith HTP-1.
        /// </summary>
        public MonolithHTP1FilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for Monoprice Monolith HTP-1.
        /// </summary>
        public MonolithHTP1FilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        public override void Export(string path) {
            StringBuilder result = new StringBuilder("[");
            PeakingEQ noFilter = new PeakingEQ(SampleRate, 100, 0, 0);
            for (int i = 0; i < Bands; i++) {
                if (i != 0) {
                    result.Append(',');
                }
                result.Append("{\"checksum\":null,\"channels\":{");
                for (int ch = 0; ch < Channels.Length; ch++) {
                    string label = "unknown";
                    ReferenceChannel channel = Channels[ch].reference;
                    if (channelLabels.ContainsKey(channel)) {
                        label = channelLabels[channel];
                    }

                    if (ch != 0) {
                        result.Append(',');
                    }
                    BiquadFilter[] filters = ((IIRChannelData)Channels[ch]).filters;
                    BiquadFilter filter = i < filters.Length && filters[i] != null ? filters[i] : noFilter;
                    result.Append($"\"{label}\":{{\"Fc\":{Parse(filter.CenterFreq)},\"gaindB\":{Parse(filter.Gain)}," +
                        $"\"Q\":{Parse(filter.Q)},\"FilterType\":0}}");
                }
                result.Append($"}},\"name\":\"BAND {i + 1}\",\"valid\":false}}");
            }
            File.WriteAllText(path, result.Append(']').ToString());
        }

        /// <summary>
        /// All values in this config file are at 1 decimal precision.
        /// </summary>
        static string Parse(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

        /// <summary>
        /// The corresponding JSON label for each supported channel.
        /// </summary>
        readonly static Dictionary<ReferenceChannel, string> channelLabels = new Dictionary<ReferenceChannel, string> {
            [ReferenceChannel.FrontLeft] = "lf",
            [ReferenceChannel.FrontRight] = "rf",
            [ReferenceChannel.FrontCenter] = "c",
            [ReferenceChannel.ScreenLFE] = "sub1",
            [ReferenceChannel.SideLeft] = "ls",
            [ReferenceChannel.SideRight] = "rs",
            [ReferenceChannel.RearLeft] = "lb",
            [ReferenceChannel.RearRight] = "rb",
            [ReferenceChannel.TopFrontLeft] = "ltf",
            [ReferenceChannel.TopFrontRight] = "rtf",
            [ReferenceChannel.TopSideLeft] = "ltm",
            [ReferenceChannel.TopSideRight] = "rtm",
            [ReferenceChannel.TopRearLeft] = "ltr",
            [ReferenceChannel.TopRearRight] = "rtr",
            [ReferenceChannel.WideLeft] = "lw",
            [ReferenceChannel.WideRight] = "rw"
        };
    }
}