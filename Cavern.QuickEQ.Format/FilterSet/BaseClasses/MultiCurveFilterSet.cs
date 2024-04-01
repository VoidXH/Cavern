using System.IO;

using Cavern.Channels;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Additionally to the channel's <see cref="Equalizer"/>, add multiple curves for each channel.
    /// </summary>
    public class MultiCurveFilterSet : EqualizerFilterSet {
        /// <summary>
        /// Appended to each channel's main result.
        /// </summary>
        public string Postfix {  get; set; } = string.Empty;

        /// <summary>
        /// Don't export any channel's main curve, only the additional curves.
        /// </summary>
        public bool ForceAdditionals { get; set; }

        /// <summary>
        /// Channel data containing additionally exported curves.
        /// </summary>
        protected class MultiCurveChannelData : EqualizerChannelData {
            /// <summary>
            /// Extra curves and their file name postfixes to be exported additionally to the channel's result.
            /// </summary>
            public (Equalizer curve, string postfix)[] additionalCurves;
        }

        /// <summary>
        /// Additionally to the channel's <see cref="Equalizer"/>, add multiple curves for each channel.
        /// </summary>
        public MultiCurveFilterSet(int channels, int sampleRate) : base(sampleRate) => Initialize<MultiCurveChannelData>(channels);

        /// <summary>
        /// Additionally to the channel's <see cref="Equalizer"/>, add multiple curves for each channel.
        /// </summary>
        public MultiCurveFilterSet(ReferenceChannel[] channels, int sampleRate) : base(sampleRate) =>
            Initialize<MultiCurveChannelData>(channels);

        /// <summary>
        /// Setup all of the channel's curves and nothing else.
        /// </summary>
        public void SetupChannel(int channel, (Equalizer curve, string postfix)[] curves) =>
            SetupChannel(channel, null, curves, 0, 0, false, null);

        /// <summary>
        /// Setup a channel's curve with no additional gain/delay or custom name.
        /// </summary>
        public void SetupChannel(int channel, Equalizer curve, (Equalizer curve, string postfix)[] additionalCurves) =>
            SetupChannel(channel, curve, additionalCurves, 0, 0, false, null);

        /// <summary>
        /// Setup a channel's curve with additional gain/delay, but no custom name.
        /// </summary>
        public void SetupChannel(int channel, Equalizer curve, (Equalizer curve, string postfix)[] additionalCurves,
            double gain, int delaySamples) => SetupChannel(channel, curve, additionalCurves, gain, delaySamples, false, null);

        /// <summary>
        /// Setup a channel's curve with a custom name, but no additional gain/delay.
        /// </summary>
        public void SetupChannel(int channel, Equalizer curve, (Equalizer curve, string postfix)[] additionalCurves, string name) =>
            SetupChannel(channel, curve, additionalCurves, 0, 0, false, name);

        /// <summary>
        /// Setup a channel's curves with additional gain/delay, and a custom name.
        /// </summary>
        public void SetupChannel(int channel, Equalizer curve, (Equalizer curve, string postfix)[] additionalCurves,
            double gain, int delaySamples, bool switchPolarity, string name) {
            MultiCurveChannelData channelRef = (MultiCurveChannelData)Channels[channel];
            channelRef.curve = curve;
            channelRef.gain = gain;
            channelRef.delaySamples = delaySamples;
            channelRef.switchPolarity = switchPolarity;
            channelRef.name = name;
            channelRef.additionalCurves = additionalCurves;
        }

        /// <summary>
        /// Save the results to EQ curve files for each channel's each curve.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            for (int i = 0; i < Channels.Length; i++) {
                MultiCurveChannelData channelRef = (MultiCurveChannelData)Channels[i];
                if (!ForceAdditionals && channelRef.curve != null) {
                    string fileName = Path.Combine(folder, $"{fileNameBase} {Channels[i].name}{Postfix}.txt");
                    channelRef.curve.Export(fileName, 0, optionalHeader, Culture);
                }

                (Equalizer curve, string postfix)[] additionals = channelRef.additionalCurves;
                if (additionals != null) {
                    for (int j = 0; j < additionals.Length; j++) {
                        string fileName = Path.Combine(folder, $"{fileNameBase} {Channels[i].name}{additionals[j].postfix}.txt");
                        additionals[j].curve.Export(fileName, 0, optionalHeader, Culture);
                    }
                }
            }
        }
    }
}