namespace NServiceBus.Core.Tests.Serializers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
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
        public void TestIntStringDictionaryToIDictionary()
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
        public void TestCustomDictionaryToIDictionary()
        {
            var input = new CustomDictionary
            {
                { 2, 4 },
                { 3, 9 }
            };
            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<IDictionary<int, int>>(json);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(4, result[2]);
            Assert.AreEqual(9, result[3]);
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

        [Test]
        [TestCase(DateTimeKind.Local)]
        [TestCase(DateTimeKind.Unspecified)]
        [TestCase(DateTimeKind.Utc)]
        public void TestDateTime(DateTimeKind dateTimeKind)
        {
            var input = new DateTime(2010, 10, 10, 10, 10, 10, dateTimeKind);
            var json = SimpleJson.SerializeObject(input);

            var result = SimpleJson.DeserializeObject<DateTime>(json);

            Assert.AreEqual(input, result);
            Assert.AreEqual(input.Kind, result.Kind);
        }

        [Test]
        public void TestDateTimeOffset()
        {
            var input = new DateTimeOffset(2010, 10, 10, 10, 10, 10, TimeSpan.FromHours(10));
            var json = SimpleJson.SerializeObject(input);

            var result = SimpleJson.DeserializeObject<DateTimeOffset>(json);

            Assert.AreEqual(input, result);
            Assert.AreEqual(input.Offset, result.Offset);
            Assert.AreEqual(input.LocalDateTime, result.LocalDateTime);
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
        [Ignore("not supported")]
        public void TestCustomDictionary()
        {
            var input = new CustomDictionary
            {
                { 2, 4 },
                { 3, 9 }
            };
            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<CustomDictionary>(json);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(4, result[2]);
            Assert.AreEqual(9, result[3]);
        }

        [Test]
        [Ignore("not supported")]
        public void TestDictionaryToIEnumerableKVP()
        {
            var input = new Dictionary<string, string>()
            {
                {"1", "hello"},
                {"2", "world"}
            };
            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<IEnumerable<KeyValuePair<string, string>>>(json);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("hello", result.Single(kvp => kvp.Key == "1"));
            Assert.AreEqual("world", result.Single(kvp => kvp.Key == "2"));
        }

        [Test]
        [Ignore("not supported")]
        public void TestKVPEnumerable()
        {
            var input = new []
            {
                new KeyValuePair<string, string>("1", "hello"),
                new KeyValuePair<string, string>("2", "world"),
            };
            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<IEnumerable<KeyValuePair<string, string>>>(json);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("hello", result.Single(kvp => kvp.Key == "1"));
            Assert.AreEqual("world", result.Single(kvp => kvp.Key == "2"));
        }


        [Test]
        [Ignore("not supported")]
        public void TestNonGenericDictionary()
        {
            IDictionary input = new Dictionary<int, int>
            {
                {1, 2},
                {3, 4}
            };
            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<IDictionary>(json);

            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(4, result[3]);
        }

        [Test]
        [Ignore("not supported")]
        public void TestReadOnlyCollection()
        {
            var input = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>
            {
                {"hello", 11},
                {"world", 22}
            });
            var json = SimpleJson.SerializeObject(input);
            var result = SimpleJson.DeserializeObject<IReadOnlyDictionary<string, int>>(json);

            Assert.AreEqual(11, result["hello"]);
            Assert.AreEqual(22, result["world"]);
        }
    }

    class SamplePoco
    {
        public int Int { get; set; }
        public string String { get; set; }
        public Guid Guid { get; set; }
    }

    class CustomDictionary : Dictionary<int, int>
    {
    }
}