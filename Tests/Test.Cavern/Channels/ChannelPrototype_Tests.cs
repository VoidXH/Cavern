using System.Numerics;

using Cavern;
using Cavern.Channels;
using Cavern.Utilities;

namespace Test.Cavern.Channels {
    /// <summary>
    /// Tests the <see cref="ChannelPrototype"/> struct.
    /// </summary>
    [TestClass]
    public class ChannelPrototype_Tests {
        /// <summary>
        /// Tests if the standard layouts have correct channel counts.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void LayoutSizes() {
            for (int i = 1; i <= 16; i++) {
                Assert.AreEqual(i, ChannelPrototype.GetStandardMatrix(i).Length);
                Assert.AreEqual(i, ChannelPrototype.GetIndustryStandardMatrix(i).Length);
            }
        }

        /// <summary>
        /// Tests that <see cref="ChannelPrototype.ToLayoutAlternative"/>(<see cref="ChannelPrototype.ref916"/>, true) produces <see cref="Channel.SpatialPos"/>
        /// values that match the <see cref="ChannelPrototype.AlternativePositions"/> entries.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ToLayoutAlternative() {
            ReferenceChannel[] ref916 = ChannelPrototype.ref916;
            Channel[] channels = ChannelPrototype.ToLayoutAlternative(ref916);

            for (int i = 0; i < ref916.Length; i++) {
                if (ref916[i] == ReferenceChannel.ScreenLFE) {
                    continue;
                }

                if (ref916[i] != ReferenceChannel.FrontCenter) {
                    Assert.AreNotEqual(0, channels[i].Y, $"Azimuth of channel {i} ({ref916[i]}) shall not be zero.");
                }

                Vector3 expected = ChannelPrototype.AlternativePositions[(int)ref916[i]];
                Assert.IsTrue(channels[i].CubicalPos.CloseTo(expected, .1f),
                    $"Channel {i} ({ref916[i]}): CubicalPos {channels[i].CubicalPos} should match AlternativePosition {expected}.");
                Assert.IsFalse(channels[i].Y < -180 || channels[i].Y > 180, $"Azimuth of channel {i} ({ref916[i]}) has to be between -180 and 180 degrees.");
                Assert.IsFalse(channels[i].X == 0 != (channels[i].CubicalPos.Y == 0), $"Channel {i} ({ref916[i]}) can't be elevated in only one metric.");
            }
        }

        /// <summary>
        /// Tests that the X and Y rotations of channels from <see cref="ChannelPrototype.ToLayoutAlternative"/>
        /// produce the same <see cref="Channel.CubicalPos"/> when used to construct a new <see cref="Channel"/>.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ToLayoutAlternative_RotationsMatchCubicalPos() {
            ReferenceChannel[] ref916 = ChannelPrototype.ref916;
            Channel[] channels = ChannelPrototype.ToLayoutAlternative(ref916);

            for (int i = 0; i < ref916.Length; i++) {
                if (ref916[i] == ReferenceChannel.ScreenLFE) {
                    continue;
                }

                Vector3 expected = ChannelPrototype.AlternativePositions[(int)ref916[i]];
                Channel reconstructed = new Channel(channels[i].X, channels[i].Y);
                Assert.IsTrue(reconstructed.CubicalPos.CloseTo(expected, .1f),
                    $"Channel {i} ({ref916[i]}): Reconstructed CubicalPos {reconstructed.CubicalPos} should match AlternativePosition {expected}.");
            }
        }
    }
}