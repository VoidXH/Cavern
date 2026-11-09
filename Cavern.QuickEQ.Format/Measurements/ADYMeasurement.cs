using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using Cavern.Channels;
using Cavern.Format.Exceptions;
using Cavern.Format.FilterSet;
using Cavern.Format.JSON;
using Cavern.QuickEQ.Measurement;
using Cavern.Utilities;
using Cavern.Waveforms;

namespace Cavern.Format.Measurements {
    /// <summary>
    /// Represents an Audyssey MultEQ XT32 measurement file containing impulse responses for multiple channels.
    /// </summary>
    public sealed class ADYMeasurement : Measurement {
        /// <summary>
        /// Which channels are present in this format and with what name.
        /// </summary>
        public override (Channel channel, string name)[] Channels { get; protected set; }

        /// <summary>
        /// Represents an Audyssey MultEQ XT32 measurement file containing impulse responses for multiple channels.
        /// </summary>
        public ADYMeasurement() { }

        /// <summary>
        /// Import an Audyssey MultEQ XT32 measurement file containing impulse responses for multiple channels.
        /// </summary>
        public ADYMeasurement(string path) => FromFile(path, true);

        /// <summary>
        /// Import an Audyssey MultEQ XT32 measurement file containing impulse responses for multiple channels.
        /// </summary>
        public ADYMeasurement(string path, bool multithreaded) => FromFile(path, multithreaded);

        /// <summary>
        /// Import an ADY measurement file.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromFile(string path) => ParseADY(new JsonFile(File.ReadAllText(path)), true);

        /// <summary>
        /// Import an ADY measurement file.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromFile(string path, bool multithreaded) => ParseADY(new JsonFile(File.ReadAllText(path)), multithreaded);

        /// <summary>
        /// Import an ADY measurement from a string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromString(string jsonContent) => ParseADY(new JsonFile(jsonContent), true);

        /// <summary>
        /// Import an ADY measurement from a string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromString(string jsonContent, bool multithreaded) => ParseADY(new JsonFile(jsonContent), multithreaded);

        /// <summary>
        /// Parse ADY <paramref name="json"/> into <see cref="MeasuredPosition"/>s.
        /// </summary>
        void ParseADY(JsonFile json, bool multithreaded) {
            object[] detectedChannels = (object[])json["detectedChannels"];
            if (detectedChannels.Length == 0) {
                throw new CorruptionException("No channels found in ADY file");
            }

            (ReferenceChannel reference, string name)[] channelData = detectedChannels.SelectArray(channel => {
                string commandId = (string)((JsonFile)channel)["commandId"];
                ReferenceChannel reference = MultEQXT32FilterSet.MapReference(commandId);
                return (reference, commandId);
            });
            Channels = channelData.SelectArray(x => (new Channel(ChannelPrototype.Mapping[(int)x.reference]), x.name));

            string[] positions = ((JsonFile)((JsonFile)detectedChannels[0])["responseData"]).Elements.Select(x => x.Key).OrderBy(int.Parse).ToArray();

            SampleRate = 48000;
            int cacheSize = ((object[])((JsonFile)((JsonFile)detectedChannels[0])["responseData"])[positions[0]]).Length;
            if (multithreaded) {
                FFTCachePool pool = new FFTCachePool(cacheSize);
                Positions = positions.SelectArray((positionKey, position) => {
                    MultichannelWaveform data = new MultichannelWaveform(detectedChannels.SelectArray(channel => GetImpulseResponse((JsonFile)channel, positionKey)));
                    return new MeasuredPosition(position, data, SampleRate, pool);
                });
            } else {
                FFTCache cache = new FFTCache(cacheSize);
                Positions = positions.SelectArray((positionKey, position) => {
                    MultichannelWaveform data = new MultichannelWaveform(detectedChannels.SelectArray(channel => GetImpulseResponse((JsonFile)channel, positionKey)));
                    return new MeasuredPosition(position, data, SampleRate, cache);
                });
            }
        }

        /// <summary>
        /// Get the impulse response of a single channel's <paramref name="responseData"/> at a given <paramref name="positionKey"/>.
        /// </summary>
        static float[] GetImpulseResponse(JsonFile channelData, string positionKey) {
            object[] samples = (object[])((JsonFile)channelData["responseData"])[positionKey];
            float[] result = new float[samples.Length];
            for (int i = 0; i < samples.Length; i++) {
                result[i] = (float)(double)samples[i];
            }
            return result;
        }
    }
}
