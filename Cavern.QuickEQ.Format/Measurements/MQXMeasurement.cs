using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using Cavern.Channels;
using Cavern.Format.FilterSet.Consts;
using Cavern.Format.JSON;
using Cavern.QuickEQ.Measurement;
using Cavern.Utilities;
using Cavern.Waveforms;

namespace Cavern.Format.Measurements {
    /// <summary>
    /// Represents a MultEQ-X measurement file containing impulse responses for multiple channels.
    /// </summary>
    public sealed class MQXMeasurement : Measurement {
        /// <summary>
        /// Which channels are present in this format and with what name.
        /// </summary>
        public (Channel channel, string name)[] Channels { get; private set; }

        /// <summary>
        /// Represents a MultEQ-X measurement file containing impulse responses for multiple channels.
        /// </summary>
        public MQXMeasurement() { }

        /// <summary>
        /// Import a MultEQ-X measurement file containing impulse responses for multiple channels.
        /// </summary>
        public MQXMeasurement(string path) => FromFile(path, true);

        /// <summary>
        /// Import a MultEQ-X measurement file containing impulse responses for multiple channels.
        /// </summary>
        public MQXMeasurement(string path, bool multithreaded) => FromFile(path, multithreaded);

        /// <summary>
        /// Import an MQX measurement file.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromFile(string path) => ParseMQX(new JsonFile(File.ReadAllText(path)), true);

        /// <summary>
        /// Import an MQX measurement file.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromFile(string path, bool multithreaded) => ParseMQX(new JsonFile(File.ReadAllText(path)), multithreaded);

        /// <summary>
        /// Import an MQX measurement from a string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromString(string jsonContent) => ParseMQX(new JsonFile(jsonContent), true);

        /// <summary>
        /// Import an MQX measurement from a string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromString(string jsonContent, bool multithreaded) => ParseMQX(new JsonFile(jsonContent), multithreaded);

        /// <summary>
        /// Parse MQX <paramref name="json"/> into <see cref="MeasuredPosition"/>s.
        /// </summary>
        void ParseMQX(JsonFile json, bool multithreaded) {
            (string guid, string name, ReferenceChannel reference)[] channels = MQXHelpers.GetOrderedChannels(json).OrderBy(x => x.reference).ToArray();
            Channels = channels.SelectArray(x => {
                ChannelPrototype prototype = ChannelPrototype.Mapping[(int)x.reference];
                return (new Channel(prototype), x.name);
            });

            const string measurementsKey = "_measurements";
            const string dataKey = "Data";
            const string channelGuidKey = "ChannelGuid";
            const string positionGuidKey = "PositionGuid";

            object[] measurements = (object[])json[measurementsKey];
            Dictionary<string, List<(ReferenceChannel channel, float[] impulseResponse)>> parsed = new Dictionary<string, List<(ReferenceChannel, float[])>>();
            for (int i = 0; i < measurements.Length; i++) {
                JsonFile measurement = (JsonFile)measurements[i];
                string base64 = (string)measurement[dataKey];
                float[] impulseResponse = EncodingUtils.Base64ToFloatArray(base64);

                string channelGuid = (string)measurement[channelGuidKey];
                string positionGuid = (string)measurement[positionGuidKey];
                if (!parsed.ContainsKey(positionGuid)) {
                    parsed.Add(positionGuid, new List<(ReferenceChannel, float[])>());
                }
                parsed[positionGuid].Add((channels.First(x => x.guid == channelGuid).reference, impulseResponse));
            }

            SampleRate = 48000;

            int cacheSize = parsed.First().Value[0].impulseResponse.Length;
            if (multithreaded) {
                FFTCachePool pool = new FFTCachePool(cacheSize);
                Positions = parsed.SelectArray((x, mic) => {
                    x.Value.Sort();
                    return new MeasuredPosition(mic, new MultichannelWaveform(x.Value.SelectArray(x => x.impulseResponse)), SampleRate, pool);
                });
            } else {
                FFTCache cache = new FFTCache(cacheSize);
                Positions = parsed.SelectArray((x, mic) => {
                    x.Value.Sort();
                    return new MeasuredPosition(mic, new MultichannelWaveform(x.Value.SelectArray(x => x.impulseResponse)), SampleRate, cache);
                });
            }
        }
    }
}
