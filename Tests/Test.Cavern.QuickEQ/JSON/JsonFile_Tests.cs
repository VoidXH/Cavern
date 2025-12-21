using Cavern.Format.JSON;

namespace Test.Cavern.QuickEQ.JSON {
    /// <summary>
    /// Tests the <see cref="JsonFile"/> class.
    /// </summary>
    [TestClass]
    public class JsonFile_Tests {
        /// <summary>
        /// Tests if a file is parsed successfully.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void TestParse() {
            JsonFile parsed = new JsonFile(raw);
            Assert.AreEqual(256, parsed["num"]);
            Assert.AreEqual("dog", ((object[])((JsonFile)parsed["child"])["arr"])[2]);
        }

        /// <summary>
        /// Tests if valid strings are created from <see cref="JsonFile"/>s.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void TestReparse() {
            JsonFile parsed = new JsonFile(raw);
            string asString = parsed.ToString();
            Assert.AreEqual(raw, asString);
        }

        /// <summary>
        /// Test JSON object.
        /// </summary>
        const string raw = "{ \"num\": 256, \"str\": \"te\\\"s,t\", \"child\": { \"float\": 1.34, \"arr\": [ 2, 3.14, \"dog\" ] } }";
    }
}