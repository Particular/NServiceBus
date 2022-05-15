namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using NUnit.Framework;

    [TestFixture]
    class DataBusDeserializerTests
    {
        [Test]
        public void Should_deserialized_with_the_serializer_used()
        {
            var jsonSerializer = new SystemJsonDataBusSerializer();
            var deserializer = new DataBusDeserializer(jsonSerializer, null);
            var somePropertyValue = "test";

            using (var stream = new MemoryStream())
            {
                jsonSerializer.Serialize(somePropertyValue, stream);
                stream.Position = 0;

                var deserializedProperty = deserializer.Deserialize(jsonSerializer.Name, typeof(string), stream);

                Assert.AreEqual(somePropertyValue, deserializedProperty);
            }
        }

        [Test]
        public void Should_throw_if_serializer_used_not_available()
        {
            var jsonSerializer = new SystemJsonDataBusSerializer();
            var deserializer = new DataBusDeserializer(jsonSerializer, null);
            var somePropertyValue = "test";

            using (var stream = new MemoryStream())
            {
                jsonSerializer.Serialize(somePropertyValue, stream);
                stream.Position = 0;

                var ex = Assert.Throws<Exception>(() => deserializer.Deserialize("other-serializer-not-configured", typeof(string), stream));

                StringAssert.Contains("other-serializer-not-configured", ex.Message);
            }
        }

        [Test]
        public void Should_try_main_and_fallback_when_serializer_used_not_known()
        {
            var jsonSerializer = new SystemJsonDataBusSerializer();
#pragma warning disable CS0618
            var binarySerializer = new BinaryFormatterDataBusSerializer();
#pragma warning restore CS0618

            var deserializer = new DataBusDeserializer(jsonSerializer, binarySerializer);
            var somePropertyValue = "test";

            using (var stream = new MemoryStream())
            {
                binarySerializer.Serialize(somePropertyValue, stream);
                stream.Position = 0;

                var deserializedProperty = deserializer.Deserialize(null, typeof(string), stream);

                Assert.AreEqual(somePropertyValue, deserializedProperty);
            }
        }

        [Test]
        public void Should_throw_when_both_main_and_fallback_cant_deserialize()
        {
            var jsonSerializer = new SystemJsonDataBusSerializer();
#pragma warning disable CS0618
            var binarySerializer = new BinaryFormatterDataBusSerializer();
#pragma warning restore CS0618

            var deserializer = new DataBusDeserializer(jsonSerializer, binarySerializer);

            using (var stream = new MemoryStream())
            {
                stream.Write(new byte[5], 0, 5);
                stream.Position = 0;

                Assert.Throws<SerializationException>(() => deserializer.Deserialize(null, typeof(string), stream));
            }
        }
    }
}