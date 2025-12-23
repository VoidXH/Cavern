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
        /// Tests if object arrays are parsed successfully.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void TestObjectArray() {
            JsonFile parsed = new JsonFile(objectArray);
            object[] objects = (object[])parsed["objects"];
            Assert.AreEqual(3, objects.Length);
            Assert.AreEqual(1, ((JsonFile)objects[0])["id"]);
            Assert.AreEqual("Second", ((JsonFile)objects[1])["name"]);
        }

        /// <summary>
        /// Tests if a value is assigned properly.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void TestValueAssignment()
        {
            JsonFile data = new JsonFile {
                { "X", true },
                { "Y", "John" }
            };

            data["X"] = false;

            Assert.AreEqual(2, data.Elements.Count);
            Assert.AreEqual(false, data["X"]);
            Assert.AreEqual("John", data["Y"]);
        }


        /// <summary>
        /// Test JSON for basic operations.
        /// </summary>
        const string raw = "{ \"num\": 256, \"str\": \"te\\\"s,t\", \"child\": { \"float\": 1.34, \"arr\": [ 2, 3.14, \"dog\" ] } }";

        /// <summary>
        /// Test JSON for object array tests.
        /// </summary>
        const string objectArray = "{ \"objects\": [ { \"id\": 1, \"name\": \"First\" }, { \"id\": 2, \"name\": \"Second\" }, { \"id\": 3, \"name\": \"Third\" } ] }";
    }
}