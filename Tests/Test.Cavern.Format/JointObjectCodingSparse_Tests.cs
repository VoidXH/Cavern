using System.Reflection;
using Cavern.Format;

namespace Test.Cavern.Format;

[TestClass]
public class JointObjectCodingSparse_Tests {
    [TestMethod]
    public void SparsePredictorsFollowBandsAndResetAtEachDataPoint() {
        Type type = typeof(EnhancedAC3Reader).Assembly.GetType("Cavern.Format.Decoders.EnhancedAC3.JointObjectCoding", true);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        foreach (int channels in new[] { 5, 7 }) {
            foreach (byte quantization in new byte[] { 0, 1 }) {
                using IDisposable decoder = (IDisposable)Activator.CreateInstance(type);
                type.GetProperty("ChannelCount").SetValue(decoder, channels);
                type.GetProperty("ObjectCount").SetValue(decoder, 1);
                type.GetMethod("UpdateCache").Invoke(decoder, null);
                object Field(string name) => type.GetField(name, flags).GetValue(decoder);
                ((byte[])Field("quantizationTable"))[0] = quantization;
                ((byte[])Field("bands"))[0] = 4;
                ((int[])Field("dataPoints"))[0] = 2;
                int[][] codedChannels = ((int[][][])Field("jocChannel"))[0];
                int[][] codedValues = ((int[][][])Field("jocVector"))[0];
                float[][][] matrix = ((float[][][][])Field("mixMatrix"))[0];
                int center = quantization == 0 ? 48 : 96;
                int modulus = center * 2;
                float step = quantization == 0 ? .2f : .1f;
                int[] selected = { channels - 1, 1, 3, 0 };
                int[] coefficients = { 10, -7, 4, -12 };
                for (int dp = 0; dp < 2; dp++) {
                    int previousChannel = 0, previousValue = quantization == 0 ? 50 : 100;
                    for (int band = 0; band < 4; band++) {
                        int channel = selected[(band + dp) % 4];
                        int value = center + coefficients[(band + dp) % 4];
                        codedChannels[dp][band] = band == 0 ? channel : (channel - previousChannel + channels) % channels;
                        codedValues[dp][band] = (value - previousValue + modulus) % modulus;
                        previousChannel = channel;
                        previousValue = value;
                    }
                }
                type.GetMethod("DecodeSparse", flags).Invoke(decoder, new object[] { 0, matrix, center });
                type.GetMethod("Dequantize", flags).Invoke(decoder, new object[] { 0, matrix, center, step });
                for (int dp = 0; dp < 2; dp++) {
                    for (int band = 0; band < 4; band++) {
                        for (int channel = 0; channel < channels; channel++) {
                            float expected = channel == selected[(band + dp) % 4] ? coefficients[(band + dp) % 4] * step : 0;
                            Assert.AreEqual(expected, matrix[dp][channel][band], 1e-6f,
                                $"channels={channels}, quantization={quantization}, dp={dp}, band={band}, channel={channel}");
                        }
                    }
                }
            }
        }
    }
}
