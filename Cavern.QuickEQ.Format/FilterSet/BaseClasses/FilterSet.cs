using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format.Common;
using Cavern.Format.FilterSet.Enums;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// A filter set containing equalization info for each channel of a system.
    /// </summary>
    public abstract partial class FilterSet : IExportable {
        /// <summary>
        /// Applied filters for each channel in the configuration file.
        /// </summary>
        public ChannelData[] Channels { get; protected set; }

        /// <summary>
        /// Sample rate of the filter set.
        /// </summary>
        public int SampleRate { get; private set; }

        /// <summary>
        /// The number of channels to EQ.
        /// </summary>
        public int ChannelCount => Channels.Length;

        /// <summary>
        /// Some targets use the user's culture in their exports. These targets should override this value with
        /// the desired export culture, <see cref="CultureInfo.CurrentCulture"/> by default.
        /// </summary>
        public CultureInfo Culture { get; protected set; } = CultureInfo.InvariantCulture;

        /// <inheritdoc/>
        public virtual string FileExtension => "txt";

        /// <summary>
        /// In what to measure delays when exporting.
        /// </summary>
        public virtual DelayUnit DelayUnits => DelayUnit.Milliseconds;

        /// <summary>
        /// A filter set containing equalization info for each channel of a system on a given sample rate.
        /// </summary>
        protected FilterSet(int sampleRate) => SampleRate = sampleRate;

        /// <inheritdoc/>
        public abstract void Export(string path);

        /// <summary>
        /// Convert the filter set to convolution impulse responses to be used with e.g. a <see cref="MultichannelConvolver"/>.
        /// </summary>
        public abstract MultichannelWaveform GetConvolutionFilter(int sampleRate, int convolutionLength);

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected virtual string GetLabel(int channel) => Channels[channel].name ?? "CH" + (channel + 1);

        /// <summary>
        /// Add extra information for a channel that can't be part of the filter files to be written in the root file.
        /// </summary>
        /// <returns>Any information was exported.</returns>
        protected virtual bool RootFileExtension(int channel, StringBuilder result) => false;

        /// <summary>
        /// Get the delay for a given <paramref name="channel"/> in milliseconds instead of samples.
        /// </summary>
        public double GetDelay(int channel) => Channels[channel].delaySamples * 1000.0 / SampleRate;

        /// <summary>
        /// Set the <paramref name="delay"/> in samples for a given <paramref name="channel"/>.
        /// </summary>
        public void OverrideDelay(int channel, int delay) => Channels[channel].delaySamples = delay;

        /// <summary>
        /// Initialize the data holders of <see cref="Channels"/> with the default <see cref="ReferenceChannel"/>s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Initialize<T>(int channels) where T : ChannelData, new() {
            Channels = new T[channels];
            ReferenceChannel[] matrix = ChannelPrototype.GetStandardMatrix(channels);
            for (int i = 0; i < channels; i++) {
                Channels[i] = new T {
                    reference = matrix[i]
                };
            }
        }

        /// <summary>
        /// Create the file with gain/delay/polarity info as the root document that's saved in the save dialog.
        /// </summary>
        protected void CreateRootFile(string path, string filterFileExtension) {
            string fileNameBase = Path.GetFileNameWithoutExtension(path);
            StringBuilder result = new StringBuilder();
            bool hasDelays = false;
            for (int i = 0, c = Channels.Length; i < c; i++) {
                hasDelays |= RootFileChannelHeader(i, result, false);
            }
            if (result.Length != 0) {
                File.WriteAllText(path, (hasDelays ?
                    $"Set up levels and delays by this file. Load \"{fileNameBase} <channel>.{filterFileExtension}\" files as EQ." :
                    $"Set up levels by this file. Load \"{fileNameBase} <channel>.{filterFileExtension}\" files as EQ.") +
                    System.Environment.NewLine + result);
            }
        }

        /// <summary>
        /// Get the gain of each channel in decibels, between the allowed limits of the output format.
        /// If the gains are not out of range, they will be returned as-is.
        /// </summary>
        protected double[] GetGains(double min, double max) {
            double[] result = Channels.Select(x => {
                if (x is IIRFilterSet.IIRChannelData iirData) {
                    return iirData.gain;
                } else if (x is EqualizerFilterSet.EqualizerChannelData eqData) {
                    return eqData.gain;
                } else if (x is FIRFilterSet.FIRChannelData) {
                    throw new FIRGainException();
                } else {
                    throw new NotImplementedException();
                }
            }).ToArray();
            double minFound = double.MaxValue, maxFound = double.MinValue;
            for (int i = 0; i < result.Length; i++) {
                result[i] = ((IIRFilterSet.IIRChannelData)Channels[i]).gain;
                if (minFound > result[i]) {
                    minFound = result[i];
                }
                if (maxFound < result[i]) {
                    maxFound = result[i];
                }
            }
            if (minFound >= min && maxFound <= max) {
                return result;
            }

            double avg = QMath.Average(result);
            for (int i = 0; i < result.Length; i++) {
                result[i] = Math.Clamp(result[i] - avg, min, max);
            }

            return result;
        }

        /// <summary>
        /// Initialize the data holders of <see cref="Channels"/> with the correct <see cref="ReferenceChannel"/>s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Initialize<T>(ReferenceChannel[] channels) where T : ChannelData, new() {
            Channels = new T[channels.Length];
            for (int i = 0; i < channels.Length; i++) {
                Channels[i] = new T {
                    reference = channels[i]
                };
            }
        }

        /// <summary>
        /// Insert channel header and basic information to a root file.
        /// </summary>
        /// <returns>Delay information was exported. If any other information was exported should be checked by checking
        /// if the length of <paramref name="result"/> has changed.</returns>
        /// <param name="channel">Channel index</param>
        /// <param name="result">Append the header to this</param>
        /// <param name="force">Write the channel name even if nothing would be exported for it</param>
        protected bool RootFileChannelHeader(int channel, StringBuilder result, bool force) {
            StringBuilder extension = new StringBuilder();
            if (!RootFileExtension(channel, extension) && !force) {
                return false;
            }

            result.AppendLine(string.Empty);
            string chName = GetLabel(channel);
            result.AppendLine(chName);
            result.AppendLine(new string('=', chName.Length));
            result.Append(extension);
            if (Channels[channel].delaySamples != 0) {
                switch (DelayUnits) {
                    case DelayUnit.Milliseconds:
                        result.AppendLine($"Delay: {QMath.ToStringLimitDecimals(GetDelay(channel), 2)} ms");
                        break;
                    case DelayUnit.Centimeters:
                        float seconds = Channels[channel].delaySamples / (float)SampleRate;
                        float centimeters = seconds * Source.SpeedOfSound * 100;
                        result.AppendLine($"Delay: {centimeters:0} cm");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                return true;
            }
            return false;
        }
    }
}