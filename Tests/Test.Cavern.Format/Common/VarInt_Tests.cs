using Cavern.Format.Common;

namespace Test.Cavern.Format {
    /// <summary>
    /// Tests the <see cref="VarInt"/> class.
    /// </summary>
    [TestClass]
    public class Clip_Tests {
        /// <summary>
        /// Tests if a value can be written into a <see cref="Stream"/> and the same can be read.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Loopback() {
            const long testValue = 12345678900;
            MemoryStream stream = new MemoryStream();
            VarInt.Fill(stream, 6, testValue);
            stream.Position = 0;
            Assert.AreEqual(testValue, VarInt.ReadValue(stream));
        }

        /// <summary>
        /// Tests if a tag can be written into a <see cref="Stream"/> and the same can be read.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void TagLoopback() {
            const int testTag4 = 0x1A45DFA3;
            const int testTag2 = 0x4286;
            MemoryStream stream = new MemoryStream();
            VarInt.WriteTag(stream, testTag4);
            VarInt.WriteTag(stream, testTag2);
            stream.Position = 0;
            Assert.AreEqual(testTag4, VarInt.ReadTag(stream));
            Assert.AreEqual(testTag2, VarInt.ReadTag(stream));
        }

        /// <summary>
        /// Tests if a value with automatic length can be written into a <see cref="Stream"/> and the same can be read.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void WriteLoopback() {
            const byte testByte = 15;
            const short testShort = 8000;
            const int testInt = 123456;
            MemoryStream stream = new MemoryStream();
            VarInt.Write(stream, testByte);
            VarInt.Write(stream, testShort);
            VarInt.Write(stream, testInt);
            stream.Position = 0;
            Assert.AreEqual(testByte, VarInt.ReadValue(stream));
            Assert.AreEqual(testShort, VarInt.ReadValue(stream));
            Assert.AreEqual(testInt, VarInt.ReadValue(stream));
        }
    }
}