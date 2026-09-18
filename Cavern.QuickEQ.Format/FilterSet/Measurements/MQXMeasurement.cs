using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Format.FilterSet.Consts;
using Cavern.Format.JSON;
using Cavern.QuickEQ;
using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Measurement;
using Cavern.Utilities;
using Cavern.Utilities.Threading;

namespace Cavern.Format.FilterSet.Measurements {
    /// <summary>
    /// Represents a MultEQ-X measurement file containing impulse responses for multiple channels.
    /// </summary>
    public class MQXMeasurement {
        /// <summary>
        /// Sample rate of the measurement data.
        /// </summary>
        public int SampleRate { get; } = 48000;

        /// <summary>
        /// Measurements grouped by microphone position, in the order the positions appear in the file.
        /// </summary>
        public MeasuredPosition[] Positions { get; private set; }

        /// <summary>
        /// Import an MQX measurement file.
        /// </summary>
        public void FromFile(string path) => ParseMQX(new JsonFile(File.ReadAllText(path)));

        /// <summary>
        /// Import an MQX measurement from a string.
        /// </summary>
        public void FromString(string jsonContent) => ParseMQX(new JsonFile(jsonContent));

        /// <summary>
        /// Parse MQX <paramref name="json"/> into <see cref="MeasuredPosition"/>s.
        /// </summary>
        void ParseMQX(JsonFile json) {
            Dictionary<string, ReferenceChannel> channelMapping = MQXHelpers.GetChannelMapping(json);

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
                parsed[positionGuid].Add((channelMapping[channelGuid], impulseResponse));
            }

            Positions = parsed.SelectArray((x, mic) => {
                x.Value.Sort();
                FFTCachePool pool = new FFTCachePool(x.Value[0].impulseResponse.Length);
                Equalizer[] frequencyResponses = new Equalizer[x.Value.Count];
                VerboseImpulseResponse[] impulseResponses = new VerboseImpulseResponse[x.Value.Count];
                Parallelizer.For(0, x.Value.Count, i => {
                    FFTCache cache = pool.Lease();
                    Complex[] transferFunction = x.Value[i].impulseResponse.FFT(cache);
                    pool.Return(cache);
                    frequencyResponses[i] = EQGenerator.FromTransferFunctionOptimized(transferFunction, SampleRate);
                    impulseResponses[i] = new VerboseImpulseResponse(x.Value[i].impulseResponse);
                });
                return new MeasuredPosition(mic, frequencyResponses, impulseResponses);
            });
        }
    }
}
