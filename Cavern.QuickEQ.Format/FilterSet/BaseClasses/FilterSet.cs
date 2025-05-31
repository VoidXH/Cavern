using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format.Common;
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
        /// Set the <paramref name="delay"/> in samples for a given <paramref name="channel"/>.
        /// </summary>
        public void OverrideDelay(int channel, int delay) => Channels[channel].delaySamples = delay;

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected virtual string GetLabel(int channel) => Channels[channel].name ?? "CH" + (channel + 1);

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
                result.AppendLine("Delay: " + QMath.ToStringLimitDecimals(GetDelay(channel), 2));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add extra information for a channel that can't be part of the filter files to be written in the root file.
        /// </summary>
        /// <returns>Any information was exported.</returns>
        protected virtual bool RootFileExtension(int channel, StringBuilder result) => false;

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
        /// Get the delay for a given channel in milliseconds instead of samples.
        /// </summary>
        protected double GetDelay(int channel) => Channels[channel].delaySamples * 1000.0 / SampleRate;

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
    }
}