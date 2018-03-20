namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using SimpleJson;

    [TestFixture]
    public class SimpleJsonTests
    {
        [Test]
        public void TestArray()
        {
            var input = new[]
            {
                "a",
                "b",
                "c"
            };
            var json = SimpleJson.SerializeObject(input);

            var result = SimpleJson.DeserializeObject<string[]>(json);

            Assert.IsNotNull(result);
            CollectionAssert.AreEquivalent(input, result);
        }

        [Test]
        public void TestEnumerable()
        {
            var input = new[]
            {
                "a",
                "b",
                "c"
            };
            var json = SimpleJson.SerializeObject(input);

            var result = SimpleJson.DeserializeObject<IEnumerable<string>>(json);

            Assert.IsNotNull(result);
            CollectionAssert.AreEquivalent(input, result);
        }

        [Test]
        [Ignore("not supported")]
        public void TestHashSet()
        {
            var set = new HashSet<string>
            {
                "hello", "world"
            };
            var json = SimpleJson.SerializeObject(set);

            var result = SimpleJson.DeserializeObject<HashSet<string>>(json);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("hello"));
            Assert.IsTrue(result.Contains("world"));
        }

        [Test]
        public void TestIntStringDictionary()
        {
            var input = new Dictionary<int, string>()
            {
                {1, "hello"},
                {2, "world"}
            };
            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<Dictionary<int, string>>(json);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("hello", result[1]);
            Assert.AreEqual("world", result[2]);
        }

        [Test]
        public void TestIntStringIDictionary()
        {
            var input = new Dictionary<int, string>()
            {
                {1, "hello"},
                {2, "world"}
            };
            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<IDictionary<int, string>>(json);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("hello", result[1]);
            Assert.AreEqual("world", result[2]);
        }

        [Test]
        public void TestStringStringDictionary()
        {
            var input = new Dictionary<string, string>()
            {
                {"1", "hello"},
                {"2", "world"}
            };
            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<Dictionary<string, string>>(json);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("hello", result["1"]);
            Assert.AreEqual("world", result["2"]);
        }

        [Test]
        public void TestStringObjectDictionary()
        {
            var input = new Dictionary<string, SamplePoco>()
            {
                {"obj1", new SamplePoco { Guid = Guid.NewGuid(), Int = 21, String = "abc"}},
                {"obj2", new SamplePoco { Guid = Guid.NewGuid(), Int = 42, String = "xyz"}}
            };
            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<Dictionary<string, SamplePoco>>(json);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(input["obj1"].Guid, result["obj1"].Guid);
            Assert.AreEqual(input["obj1"].Int, result["obj1"].Int);
            Assert.AreEqual(input["obj1"].String, result["obj1"].String);
            Assert.AreEqual(input["obj2"].Guid, result["obj2"].Guid);
            Assert.AreEqual(input["obj2"].Int, result["obj2"].Int);
            Assert.AreEqual(input["obj2"].String, result["obj2"].String);
        }

        [Test]
        public void TestPocoClass()
        {
            var input = new SamplePoco
            {
                Guid = Guid.NewGuid(),
                Int = 21,
                String = "abc"
            };

            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<SamplePoco>(json);

            Assert.AreEqual(input.Guid, result.Guid);
            Assert.AreEqual(input.Int, result.Int);
            Assert.AreEqual(input.String, result.String);
        }

        //TODO: ReadOnlyCollections
        //TODO: Enumerable<KeyValuePair>
    }

    class SamplePoco
    {
        public int Int { get; set; }
        public string String { get; set; }
        public Guid Guid { get; set; }
    }
}